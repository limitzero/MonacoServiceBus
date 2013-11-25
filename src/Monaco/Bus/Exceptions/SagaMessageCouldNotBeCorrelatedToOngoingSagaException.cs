using System;

namespace Monaco.Bus.Exceptions
{
	/// <summary>
	/// Exception that is thrown when the message can not be correlated to the ongoing saga for processing.
	/// </summary>
	public class SagaMessageCouldNotBeCorrelatedToOngoingSagaException : ApplicationException
	{
		private const string _message =
			"The following message '{0}' could not be correlated to the current saga '{1}' for instance '{2}' by the choosen correlation condition(s) from the persistance storage.";

		public SagaMessageCouldNotBeCorrelatedToOngoingSagaException(Type theMessage, Type theSaga, Guid instanceId)
			: base(string.Format(_message, theMessage.FullName, theSaga.FullName, instanceId))
		{
		}
	}
}