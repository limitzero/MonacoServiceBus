using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;

namespace Monaco.Visualizations.Providers
{
	public class MSDirectedGraphGeneratorEngine : IDotEngine
	{
		public string Run(GraphvizImageType imageType, string dot, string outputFileName)
		{
			return BuildDirectedGraph(dot);
		}

		private string BuildDirectedGraph(string dot)
		{
			var result = string.Empty;

			dot = dot.Replace("digraph G {", string.Empty);
			dot = dot.Replace("}", string.Empty);
			dot = dot.Trim();

			var lines = dot.Split(";".ToCharArray());

			var graph = BuildGraph(lines);

			XmlSerializer serializer = new XmlSerializer(typeof(MSDirectedGraph));
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, graph);
				stream.Seek(0, SeekOrigin.Begin);
				result = Encoding.Default.GetString(stream.ToArray());
			}

			result = result.Replace("&quot;", string.Empty);

			return result;
		}

		private MSDirectedGraph BuildGraph(IEnumerable<string> lines)
		{
			var graph = new MSDirectedGraph();

			graph = ReadGraph(lines);

			return graph;
		}

		public MSDirectedGraph ReadGraph(IEnumerable<string> lines)
		{
			MSDirectedGraph graph = new MSDirectedGraph();

			var nodes = new List<string>();
			var links = new List<string>();

			foreach (var line in lines)
			{
				if (line.Contains("[") & !line.Contains("->"))
				{
					nodes.Add(line.Trim());
				}

				if (line.Contains("->"))
				{
					links.Add(line.Trim());
				}
			}

			foreach (var node in nodes)
			{
				var directedNode = BuildNode(node);
				graph.Nodes.Add(directedNode);
			}

			BuildLinks(graph, links);

			return graph;
		}

		private MSDirectedNode BuildNode(string nodeDefinition)
		{
			MSDirectedNode node = new MSDirectedNode();

			var definition = ExtractDirectedGraphNodeDefinition(nodeDefinition);
			var parts = definition.Split(",".ToCharArray());

			var nodeAlias = string.Empty;

			// find the numerical representation of the node (this is the alias):
			foreach (var character in nodeDefinition.ToCharArray())
			{
				if (character == " ".ToCharArray()[0]) break;
				nodeAlias += character;
			}

			node.NodeAlias = nodeAlias.Trim();

			foreach (var part in parts)
			{
				var pair = this.ExtractNameValuePair(part);

				if (pair.ContainsKey("label"))
				{
					var name = string.Empty;
					pair.TryGetValue("label", out name);
					node.Name = name.Replace("\"", string.Empty);
				}

				if (pair.ContainsKey("fillcolor"))
				{
					var background = string.Empty;
					pair.TryGetValue("fillcolor", out background);

					try
					{
						System.Drawing.Color color =
										System.Drawing.ColorTranslator.FromHtml(background);
						node.FillColor = color.Name;
					}
					catch
					{
						node.FillColor = background;
					}
				}

				if (!string.IsNullOrEmpty(node.Name))
				{
					if (node.Name.Trim().ToLower() == "start")
						node.FillColor = "Green";

					if (node.Name.Trim().ToLower() == "end")
						node.FillColor = "Blue";
				}
			}

			return node;
		}

		private void BuildLinks(MSDirectedGraph graph, IEnumerable<string> links)
		{
			foreach (var link in links)
			{
				var label = string.Empty;
				var directedness = new Dictionary<string, string>();
				directedness = this.ExtractLinkDefintion(link, out label);

				var source = (from node in graph.Nodes
							  where node.NodeAlias == directedness.Keys.ToList()[0]
							  select node).FirstOrDefault();

				var target = (from node in graph.Nodes
							  where node.NodeAlias == directedness.Values.ToList()[0]
							  select node).FirstOrDefault();

				var directedLink = new MSDirectedLink()
									{
										Label = label,
										SourceNode = source,
										TargetNode = target
									};

				graph.Links.Add(directedLink);
			}
		}

		private Dictionary<string, string> ExtractLinkDefintion(string line, out string label)
		{
			var link = new Dictionary<string, string>();
			label = string.Empty;

			var linkDefinition = string.Empty;

			foreach (var character in line.ToCharArray())
			{
				if (character == "[".ToCharArray()[0]) break;
				linkDefinition += character;
			}

			var directedness = linkDefinition.Split("->".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			link.Add(directedness[0].Trim(), directedness[1].Trim());

			var definition = ExtractDirectedGraphNodeDefinition(line);
			var parts = ExtractNameValuePair(definition);

			parts.TryGetValue("label", out label);

			return link;
		}

		private string ExtractDirectedGraphNodeDefinition(string line)
		{
			var startPos = line.IndexOf("[") + 1;
			var endPos = line.IndexOf("]") - 1;
			var definition = line.Substring(startPos, endPos - startPos);
			return definition;
		}

		private IDictionary<string, string> ExtractNameValuePair(string nameValuePair)
		{
			var result = new Dictionary<string, string>();

			if (nameValuePair.Contains("="))
			{
				var parts = nameValuePair.Split("=".ToCharArray());
				if (parts.Length == 2)
				{
					result.Add(parts[0].Trim(), parts[1].Trim());
				}
			}

			return result;
		}
	}

	[XmlRoot(Namespace = "http://schemas.microsoft.com/vs/2009/dgml",ElementName = "DirectedGraph")]
	public class MSDirectedGraph
	{
		[XmlArrayItem(ElementName = "Node")]
		public List<MSDirectedNode> Nodes { get; set; }

		[XmlArrayItem(ElementName = "Link")]
		public List<MSDirectedLink> Links { get; set; }

		public MSDirectedGraph()
		{
			this.Links = new List<MSDirectedLink>();
			this.Nodes = new List<MSDirectedNode>();
		}
	}

	[XmlRoot(ElementName = "Node")]
	public class MSDirectedNode
	{
		[XmlIgnore]
		public string NodeAlias { get; set; }

		[XmlAttribute(AttributeName = "Id")]
		public string Name { get; set; }

		[XmlAttribute]
		public string Label { get; set; }

		[XmlAttribute(AttributeName = "Background")]
		public string FillColor { get; set; }
	}

	[XmlRoot(ElementName = "Link")]
	public class MSDirectedLink
	{
		[XmlAttribute]
		public string Label { get; set; }

		[XmlAttribute]
		public string Source { get; set; }

		[XmlAttribute]
		public string Target { get; set; }

		private MSDirectedNode _sourceNode;
		[XmlIgnore]
		public MSDirectedNode SourceNode
		{
			get { return _sourceNode; }
			set
			{
				_sourceNode = value;
				this.Source = value.Name;
			}
		}

		private MSDirectedNode _targetNode;
		[XmlIgnore]
		public MSDirectedNode TargetNode
		{
			get { return _targetNode; }
			set
			{
				_targetNode = value;
				this.Target = value.Name;
			}
		}
	}

	/*
	digraph G {
	0 [shape=box, style=rounded, label="Start", fillcolor="#008000FF"];
	1 [shape=box, style=rounded, label="WaitingForProcessInvoice", fillcolor="#DCDCDCFF"];
	2 [shape=box, style=rounded, label="End", fillcolor="#FF0000FF"];
	0 -> 1 [ label="ShipmentReceived"];
	1 -> 2 [ label="InvoiceProcessed"];
	1 -> 2 [ label="PriceCalculationFailed"];
	}
	 */

}