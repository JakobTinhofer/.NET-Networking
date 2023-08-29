using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Connect.Util;

namespace LightBlueFox.Connect.Structure
{
    /// <summary>
    /// Defines a function that will be called after a <see cref="ConnectionNegotiation"/> has concluded successfully.
    /// </summary>
    /// <param name="sender">The successful negotiation.</param>
    public delegate void ValidationSuccessfulHandler(ConnectionNegotiation sender);
    /// <summary>
    /// Defines a function that will be called after a <see cref="ConnectionNegotiation"/> has failed. The failure reason is also provided.
    /// </summary>
    /// <param name="sender">The failed negotiation.</param>
    /// <param name="ex">The type of the failure.</param>
    public delegate void ValidationFailedHandler(ConnectionNegotiation sender, ValidationFailedException ex);

    // TODO: Timeout after a certain interval and try again
    /// <summary>
    /// This class handles the process of validating a connection. This is done to make sure that the node on the other end of the connection is the expected node and compatible with the protocol in use.
    /// </summary>
    public class ConnectionNegotiation: IDisposable
    {
        /// <summary>
        /// Starts a new negotiation from an established connection.
        /// </summary>
        /// <param name="c">The connection to validate.</param>
        /// <param name="position">The position from which to negotiate. This determines the order of the negotiation and needs to be different on both ends.</param>
        /// <param name="validators">The validators that should be checked before a connection is marked as valid. These need to be the same and in the same order on both ends.</param>
        /// <param name="failedCallback">The function to call if the negotiation fails.</param>
        /// <param name="successCallback">The function to call once the connection was successfully validated.</param>
        public ConnectionNegotiation(Connection c, ConnectionNegotiationPosition position, ConnectionValidator[] validators, ValidationFailedHandler failedCallback, ValidationSuccessfulHandler successCallback)
        {
            ValidationFailed = failedCallback;
            ValidationSuccessful = successCallback;
            Connection = c;
            Position = position;
            
            Validators = new List<ConnectionValidator>(validators);
            validatorQueue = new(Validators);

            
            c.ConnectionDisconnected += HandleConnectionDisconnect;
            c.MessageHandler = NegotiationMessageHandler;

            if (validators.Length == 0)
            {
                expectedMessage = NegotiationMessages.Success;
                if (position == ConnectionNegotiationPosition.Authorizer) WriteNegotiationMsg(NegotiationMessages.Success);
            }
            else
            {
                expectedMessage = position == ConnectionNegotiationPosition.Authorizer ? NegotiationMessages.Answer : NegotiationMessages.Challenge;
                if (position == ConnectionNegotiationPosition.Authorizer) WriteNegotiationMsg(NegotiationMessages.Challenge, validatorQueue.Peek().GetChallengeBytes());
            }
        }

        #region Fields & Members
        /// <summary>
        /// The function to call once the connection was successfully validated.
        /// </summary>
        public ValidationSuccessfulHandler ValidationSuccessful;
        /// <summary>
        /// The function to call if the negotiation fails.
        /// </summary>
        public ValidationFailedHandler ValidationFailed;

        /// <summary>
        /// If <c>true</c>, the negotiation has already ended and connection was validated.
        /// </summary>
        public bool IsFinished { get; private set; } = false;
        /// <summary>
        /// If <c>true</c>, the negotiation failed and was aborted.
        /// </summary>
        public bool HasFailed { get; private set; } = false;

        /// <summary>
        /// The connection that is being validated.
        /// </summary>
        public readonly Connection Connection;
        
        /// <summary>
        /// The position from which to negotiate. This determines the order of the negotiation and needs to be different on both ends.
        /// </summary>
        
        public readonly ConnectionNegotiationPosition Position;
        /// <summary>
        /// The validators that should be checked before a connection is marked as valid.
        /// </summary>
        public readonly IReadOnlyList<ConnectionValidator> Validators;

        /// <summary>
        /// Makes sure that the validators are checked one by one in the correct order.
        /// </summary>
        private readonly Queue<ConnectionValidator> validatorQueue;

        /// <summary>
        /// The negotiation process needs to follow a very rigid order.
        /// </summary>
        private NegotiationMessages expectedMessage;
        #endregion

        #region Event Handlers
        /// <summary>
        /// Fail the negotiation if the connection is closed unexpectedly.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="ex"></param>
        private void HandleConnectionDisconnect(Connection c, Exception? ex)
        {
            FailNegotiation(new(ValidationFailure.Disconnect), false);
        }
        #endregion

        #region Outcomes
        private void FailNegotiation(ValidationFailedException ex, bool send = true)
        {
            if (IsFinished) return;
            IsFinished = true;
            Connection.ConnectionDisconnected -= HandleConnectionDisconnect;
            if (send) Connection.WriteMessage(new byte[2] { (byte)NegotiationMessages.VerificationAborted, (byte)ex.FailureType });
            ValidationFailed.Invoke(this, ex);
            Connection.CloseConnection();
        }

        private void SucceedNegotiation()
        {
            if (IsFinished) return;
            IsFinished = true;
            Connection.ConnectionDisconnected -= HandleConnectionDisconnect;
            Connection.MessageHandler = null;
            ValidationSuccessful.Invoke(this);
        }
        #endregion

        #region Writing Helpers
        private void WriteNegotiationMsg(NegotiationMessages m, byte[]? msg = null)
        {
            if(msg == null)
            {
                Connection.WriteMessage(new byte[1] { (byte)m });
            }
            else
            {
                byte[] payload = new byte[msg.Length + 1];
                payload[0] = (byte)m;
                Array.Copy(msg, 0, payload, 1, msg.Length);
                Connection.WriteMessage(payload);
            }
        }
        #endregion

        #region Message Handling
        private void NegotiationMessageHandler(ReadOnlySpan<byte> message, MessageArgs args)
        {
            NegotiationMessages mType = ((NegotiationMessages)message[0]);
            bool unexpected = mType != expectedMessage;
            var v = validatorQueue.Count == 0 ? null : validatorQueue.Dequeue();

            switch (mType)
            {
                case NegotiationMessages.VerificationAborted:
                    ValidationFailure f = ValidationFailure.Unknown;
                    if (message.Length > 2) f = (ValidationFailure)message[1];
                    FailNegotiation(new(f), false);
                    HasFailed = true;
                    return;
                case NegotiationMessages.Challenge:
                    if (v == null) FailNegotiation(new(ValidationFailure.TooManyValidators));
                    else if (unexpected) FailNegotiation(new(ValidationFailure.InvalidOrder));
                    else if (!v.ValidateChallenge(message[1..])) FailNegotiation(new(ValidationFailure.InvalidChallenge)); 
                    else { WriteNegotiationMsg(NegotiationMessages.Answer, v.GetAnswerBytes()); expectedMessage = validatorQueue.Count == 0 ? NegotiationMessages.Success : NegotiationMessages.Challenge; }
                    return;
                case NegotiationMessages.Answer:
                    if (unexpected || v == null) FailNegotiation(new(ValidationFailure.InvalidOrder));
                    else if (!v.ValidateAnswer(message[1..])) FailNegotiation(new(ValidationFailure.WrongAnswer));
                    else if (validatorQueue.Count == 0) { WriteNegotiationMsg(NegotiationMessages.Success); expectedMessage = NegotiationMessages.Success;  }
                    else WriteNegotiationMsg(NegotiationMessages.Challenge, validatorQueue.Peek().GetChallengeBytes());
                    return;
                case NegotiationMessages.Success:
                    if (unexpected || v != null) FailNegotiation(new(ValidationFailure.InsufficientValidators));
                    else {
                        if (Position == ConnectionNegotiationPosition.Challenger) WriteNegotiationMsg(NegotiationMessages.Success);
                        SucceedNegotiation();
                    }
                    return;
                default:
                    FailNegotiation(new(ValidationFailure.UnknownNegotiationMessage));
                    break;
            }
        }
        #endregion

        #region Static Wrappers

        /// <summary>
        /// Validate a connection. This call is blocking and returns only after the connection has been validated.
        /// </summary>
        /// <param name="c">The connection to validate.</param>
        /// <param name="pos">The position from which to negotiate. This determines the order of the negotiation and needs to be different on both ends.</param>
        /// <param name="validators">The validators that should be checked before a connection is marked as valid. These need to be the same and in the same order on both ends.</param>
        /// <returns>The validated connection</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// /// <exception cref="ValidationFailedException"></exception>
        public static Connection ValidateConnection(Connection c, ConnectionNegotiationPosition pos, params ConnectionValidator[] validators)
        {
            if (c.MessageHandler != null) throw new InvalidOperationException("The negotiation can only work if no message handler has been set on the connection, since otherwise messages might already have been lost!");

            TaskCompletionSource<(bool, ValidationFailedException?)> tcs = new();

            ConnectionNegotiation n = new (c, pos, validators ?? Array.Empty<ConnectionValidator>(), (c, f) => { tcs.SetResult((false, f)); }, (c) => { tcs.SetResult((true, null)); });

            var res = tcs.Task.GetAwaiter().GetResult();
            if (!res.Item1) throw res.Item2 ?? new(ValidationFailure.Unknown);
            else return c;
        }

        /// <summary>
        /// Validate a connection asynchronously.
        /// </summary>
        /// <param name="c">The connection to validate.</param>
        /// <param name="pos">The position from which to negotiate. This determines the order of the negotiation and needs to be different on both ends.</param>
        /// <param name="validators">The validators that should be checked before a connection is marked as valid. These need to be the same and in the same order on both ends.</param>
        /// <returns>The validated connection</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// /// <exception cref="ValidationFailedException"></exception>
        public static async Task<Connection> ValidateConnectionAsync(Connection c, ConnectionNegotiationPosition pos, params ConnectionValidator[] validators)
        {
            return await Task.Run<Connection>(() =>
            {
                return ValidateConnection(c, pos, validators);
            });
        }
        #endregion

        #region Abort & Disposal
        /// <summary>
        /// Stop the negotiation right now, closing the connection and calling <see cref="ValidationFailed"/>.
        /// </summary>
        public void AbortNegotiation()
        {
            if(IsFinished || HasFailed) return;
            WriteNegotiationMsg(NegotiationMessages.VerificationAborted);
            Connection.Dispose();
            FailNegotiation(new ValidationFailedException(ValidationFailure.Disconnect), false);
        }

        public void Dispose()
        {
            AbortNegotiation();
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    /// <summary>
    /// The position from which to negotiate. This determines the order of the negotiation and needs to be different on both ends.
    /// </summary>
    public enum ConnectionNegotiationPosition
    {
        /// <summary>
        /// The challenged party that first receives the challenge and answers it.
        /// </summary>
        Challenger,
        /// <summary>
        /// The party that challenges the other first and then waits for an answer.
        /// </summary>
        Authorizer
    }

    /// <summary>
    /// The message types that are sent in the negotiation
    /// </summary>
    internal enum NegotiationMessages
    {
        VerificationAborted,
        Success,
        Challenge,
        Answer
    }
}
