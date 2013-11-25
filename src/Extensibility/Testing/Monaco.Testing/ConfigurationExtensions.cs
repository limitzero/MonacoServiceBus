using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Monaco.Extensibility.Logging;
using Monaco.StateMachine;
using Monaco.Testing.Verbalizer.Impl;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static IConfiguration WithVerbalizer(this IConfiguration configuration, 
			Expression<Func<IVerbalizerConfiguration, IConfiguration>> verbalizer)
		{
			((Monaco.Configuration.Configuration)configuration)
				.BindExtensibilityAction(()=> verbalizer.Compile().Invoke(new VerbalizerConfiguration(configuration)));
			return configuration;
		}
	}

	public interface IVerbalizerConfiguration
	{
		/// <summary>
		/// This will verbalize a single instance of a state machine.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		 IConfiguration UsingVerbalizerFor<T>() where T : SagaStateMachine;

		 /// <summary>
		 /// This will verbalize all state machines that are defined on the endpoint.
		 /// </summary>
		 /// <returns></returns>
		IConfiguration UsingVerbalizerForAll();
	}

	public class VerbalizerConfiguration : IVerbalizerConfiguration
	{
		private readonly IConfiguration configuration;
	
		public VerbalizerConfiguration(IConfiguration configuration)
		{
			this.configuration = configuration;
		}

		/// <summary>
		/// This will verbalize a single instance of a state machine.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IConfiguration UsingVerbalizerFor<T>() where T : SagaStateMachine
		{
			SagaStateMachine stateMachine = null;
			if (TryGetStateMachine(configuration, typeof(T), out stateMachine))
			{
				Verbalize(configuration, new List<SagaStateMachine> { stateMachine });
			}

			return this.configuration;
		}

		/// <summary>
		/// This will verbalize all state machines that are defined on the endpoint.
		/// </summary>
		/// <returns></returns>
		public IConfiguration UsingVerbalizerForAll()
		{
			IEnumerable<SagaStateMachine> statemachines = this.configuration.Container.ResolveAll<SagaStateMachine>();
			Verbalize(configuration, statemachines);
			return this.configuration;
		}

		private static void Verbalize(IConfiguration configuration, IEnumerable<SagaStateMachine> sagaStateMachines)
		{
			StringBuilder builder = new StringBuilder();
			var verbalizer = new SagaStateMachineVerbalizer();
			var logger = configuration.Container.Resolve<ILogger>();

			foreach (var sagaStateMachine in sagaStateMachines)
			{
				try
				{
					builder.AppendLine(verbalizer.Verbalize(sagaStateMachine));
				}
				catch (Exception couldNotVerbalizeStateMachineException)
				{
					logger.LogWarnMessage(string.Format("An error has ocurred while attempting to verbalize state machine '{0}. Reason: {1}",
						sagaStateMachine, couldNotVerbalizeStateMachineException));
					continue;
				}
			}

			logger.LogDebugMessage(builder.ToString());
		}

		private static bool TryGetStateMachine(IConfiguration configuration, Type stateMachineType,
		out SagaStateMachine sagaStateMachine)
		{
			bool success = false;
			sagaStateMachine = null;

			try
			{
				configuration.Container.Register(stateMachineType);
			}
			catch
			{
				// already there
				success = true;
			}

			if (success == false)
			{
				try
				{
					sagaStateMachine = configuration.Container.Resolve(stateMachineType) as SagaStateMachine;

					if (sagaStateMachine != null)
					{
						success = true;
					}
				}
				catch
				{
				}
			}

			return success;
		}
	}
}