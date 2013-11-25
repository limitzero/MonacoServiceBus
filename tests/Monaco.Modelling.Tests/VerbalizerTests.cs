using Monaco.Modelling.Tests.Model;
using Monaco.Modelling.Verbalizer;
using Xunit;

namespace Monaco.Modelling.Tests
{
	public class VerbalizerTests : BaseCaseTest
	{
		[Fact]
		public void can_verbalize_business_process_model()
		{
			var realizer = new BusinessProcessModelVerbalizer();
			var actual = realizer.Verbalize<AcmeBusinessProcessModel>();
			Assert.Equal(this.Expected(System.Reflection.MethodInfo.GetCurrentMethod().Name), actual.Trim());
		}
	}
}