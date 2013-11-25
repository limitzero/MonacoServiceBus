namespace Monaco.Sagas
{
    /// <summary>
    /// Contract used to correlate messages together on a specific saga instance.
    /// </summary>
    /// <example>
    /// 
    /// public class MyMessage : IMessage
    /// {
    ///     public int MessageId {get; set;
    /// }
    /// 
    /// public class MySaga: 
    ///     StartedBy{MyMessage}, 
    ///     Correlated.By{MyMessage}
    /// {
    ///     private int _messageId = 100; 
    /// 
    ///     public void Consume(MyMessage message)
    ///     {
    ///         // do something with the message:
    ///     }
    /// 
    ///     public bool Correlate(MyMessage message)
    ///     {
    ///         // see if the current message matches 
    ///         // the internal message id (this will be called 
    ///         // first before the Consume(..) method above):
    ///         return message.MessageId == _messageId;
    ///     }
    /// }
    /// 
    /// </example>
    public abstract class Correlated
    {
        /// <summary>
        /// Contract used to control the correlation of a particular message
        /// to an existing set of messages for the saga.
        /// </summary>
        /// <typeparam name="TMESSAGE"></typeparam>
        public interface By<TMESSAGE> where TMESSAGE : IMessage
        {
            /// <summary>
            /// This will be called for every message that is passed into the 
            /// saga after the inititating message by the infrastructure to see if 
            /// the correlation guard condition is satisfied.
            /// </summary>
            /// <param name="message">The current message to inspect for the correlation guard condition.</param>
            /// <returns>
            /// A function returning a boolean value to determine whether or not the message satisfies 
            /// the guard condition for correlation to the initiating message.
            /// </returns>
            bool  Correlate(TMESSAGE message);
        }
    }
}