﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Monaco.StateMachine;
using Monaco.StateMachine.Internals.Impl;
using Monaco.Visualizations.Providers;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using Rhino.Mocks;

namespace Monaco.Visualizations
{
	public class SagaStateMachineVisualizer
	{
		public MemoryStream Visualize<TSagaStateMachine>()
			where TSagaStateMachine : SagaStateMachine, new()
		{
			var mockRepository = new MockRepository();
			var serviceBus = mockRepository.DynamicMock<IServiceBus>();

			var stateMachine = new TSagaStateMachine();
			stateMachine.Bus = serviceBus;
			stateMachine.Define();

			return this.Visualize(stateMachine);
		}

		public MemoryStream Visualize<TSagaStateMachine>(TSagaStateMachine sagaStateMachine)
			where TSagaStateMachine : SagaStateMachine
		{
			var stream = new MemoryStream();
			var graph = CreateGraph(sagaStateMachine);
			var binaryMap = GenerateMap(graph, sagaStateMachine);

			try
			{
				if (sagaStateMachine.Bus == null)
				{
					var mockRepository = new MockRepository();
					var serviceBus = mockRepository.DynamicMock<IServiceBus>();
					sagaStateMachine.Bus = serviceBus;
				}
				sagaStateMachine.Define();
			}
			catch
			{
				// state machine already defined...
			}

			stream = new MemoryStream(Encoding.Default.GetBytes(binaryMap));

			return stream;
		}

		private static AdjacencyGraph<string, TaggedEdge<string, string>> CreateGraph(SagaStateMachine stateMachine)
		{
			AdjacencyGraph<string, TaggedEdge<string, string>> graph = new AdjacencyGraph<string, TaggedEdge<string, string>>();
			CreateStateMachineStates(graph, stateMachine);
			CreateStateMachineEdges(graph, stateMachine);
			return graph;
		}

		private string GenerateMap(AdjacencyGraph<string, TaggedEdge<string, string>> graph,
									 SagaStateMachine stateMachine)
		{
			GraphvizAlgorithm<string, TaggedEdge<string, string>> algorithm = new GraphvizAlgorithm<string, TaggedEdge<string, string>>(graph);

			algorithm.FormatEdge += delegate(object sender, FormatEdgeEventArgs<string, TaggedEdge<string, string>> e)
			{
				e.EdgeFormatter.Label.Value = e.Edge.Tag;
			};

			algorithm.FormatVertex += delegate(object sender, FormatVertexEventArgs<string> e)
			{
				switch (e.Vertex)
				{
					case "Start":
						e.VertexFormatter.FillColor = System.Drawing.Color.Green;
						break;
					case "End":
						e.VertexFormatter.FillColor = System.Drawing.Color.Red;
						break;
					default:
						e.VertexFormatter.FillColor = System.Drawing.Color.Gainsboro;
						break;
				}

				e.VertexFormatter.Shape = GraphvizVertexShape.Box;
				e.VertexFormatter.Style = GraphvizVertexStyle.Rounded;
				e.VertexFormatter.Label = e.Vertex;

				// indicate the current state by an asterisk:
				if (stateMachine.CurrentState != null)
				{
					if (stateMachine.CurrentState.Name == e.Vertex
						&& (e.Vertex != "Start" && e.Vertex != "End"))
					{
						e.VertexFormatter.Label = e.VertexFormatter.Label + "*";
					}
				}
			};

			// get the directed graph w/optional formatting:
			//TVertex output = algorithm.Generate(new BitmapGeneratorDotEngine(), "ignored");
			string directedGraph = algorithm.Generate();

			// create the image from the directed graph:
			String output = new MSDirectedGraphGeneratorEngine().Run(GraphvizImageType.Wbmp, directedGraph, string.Empty);
			//new BitmapGeneratorDotEngine().Run(GraphvizImageType.Wbmp, directedGraph, string.Empty);
			//new SVGGenerateDotEngine().Run(GraphvizImageType.Svg, directedGraph, string.Empty);


			return output;
		}

		private static void CreateStateMachineStates(AdjacencyGraph<string, TaggedEdge<string, string>> graph,
													SagaStateMachine stateMachine)
		{
			List<string> vertexes = new List<string>();

			foreach (var triggerCondition in stateMachine.TriggerConditions)
			{
				if (vertexes.Contains(triggerCondition.Condition.State.Name) == false)
				{
					vertexes.Add(triggerCondition.Condition.State.Name);
				}

				var transitionAction = (from action in triggerCondition.Condition.MessageActions
										where action.ActionType == SagaStateMachineMessageActionType.Transition ||
											  action.ActionType == SagaStateMachineMessageActionType.Complete
										select action).FirstOrDefault();

				if (transitionAction != null)
				{
					if (vertexes.Contains(transitionAction.State.Name) == false)
					{
						vertexes.Add(transitionAction.State.Name);
					}
				}
			}

			foreach (var vertex in vertexes)
			{
				graph.AddVertex(vertex);
			}

		}

		private static void CreateStateMachineEdges(AdjacencyGraph<string, TaggedEdge<string, string>> graph,
													SagaStateMachine stateMachine)
		{
			State previousState = new State("default");

			foreach (var triggerCondition in stateMachine.TriggerConditions)
			{
				var transitionAction = (from action in triggerCondition.Condition.MessageActions
										where action.ActionType == SagaStateMachineMessageActionType.Transition ||
											  action.ActionType == SagaStateMachineMessageActionType.Complete
										select action).FirstOrDefault();

				if (transitionAction != null)
				{
					previousState = transitionAction.State;
				}

				graph.AddEdge(new TaggedEdge<string, string>(triggerCondition.Condition.State.Name,
										previousState.Name,
										triggerCondition.Condition.Event));

			}
		}

	}
}
