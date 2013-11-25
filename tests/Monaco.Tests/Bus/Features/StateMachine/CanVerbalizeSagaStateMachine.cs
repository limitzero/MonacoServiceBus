using System.IO;
using System.Reflection;
using Monaco.StateMachine.Verbalizer.Impl;
using Xunit;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class SagaVerbilzerTests
	{
		[Fact]
		public void can_verbalize_configured_statemachine()
		{
			var verbalizer = new SagaStateMachineVerbalizer();
			var results = verbalizer.Verbalize<TestStateMachine>();
			Assert.Equal(this.Expected(System.Reflection.MethodInfo.GetCurrentMethod().Name), this.Actual(results) );
		}

		private string Actual(string actual)
		{
			return actual.Replace("\r", string.Empty)
				.Replace("\n",string.Empty)
				.Replace("\t",string.Empty).Trim();
		}

		private string Expected(string method)
		{
			string expected = string.Empty;
			string resource = string.Format("{0}.{1}.{2}.txt", this.GetType().Namespace, "Cases", method);

			Assembly asm = Assembly.GetExecutingAssembly();

			using (var stream = asm.GetManifestResourceStream(resource))
			using(var reader = new StreamReader(stream))
			{
				expected = reader.ReadToEnd();
			}
			return this.Actual(expected);
		}
	}
}