using System.IO;

namespace Monaco.Modelling.Tests
{
	public abstract class BaseCaseTest
	{
		protected string Expected(string testMethodName)
		{
			string expected = string.Empty;
			string resource = string.Format("{0}.{1}.{2}.txt", this.GetType().Namespace, "Cases", testMethodName);
			using (var stream = typeof(ServiceMessageTests).Assembly.GetManifestResourceStream(resource))
			using (var reader = new StreamReader(stream))
			{
				expected = reader.ReadToEnd();
			}

			return expected.Trim();
		}
	}
}