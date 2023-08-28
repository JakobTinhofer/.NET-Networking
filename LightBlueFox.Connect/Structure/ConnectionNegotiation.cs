using LightBlueFox.Connect.Structure.Validators;
using LightBlueFox.Connect.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.Structure
{
    public delegate void ValidationSuccessfulHandler(ConnectionNegotiation sender);
    public delegate void ValidationFailedHandler(ConnectionNegotiation sender, ValidationFailedException ex);

    // TODO: Timeout after a certain interval and try again
    public class ConnectionNegotiation: IDisposable
    {
        private void cDisconnected(Connection c, Exception? ex)
        {
            validationFailed(new(ValidationFailure.Disconnect), false);
        }

        public ConnectionNegotiation(Connection c, ConnectionNegotiationPosition position, ConnectionValidator[] validators, ValidationFailedHandler failedCallback, ValidationSuccessfulHandler successCallback)
        {
            ValidationFailed = failedCallback;
            ValidationSuccessful = successCallback;
            Connection = c;
            Position = position;
            
            Validators = new List<ConnectionValidator>(validators);
            validatorQueue = new(Validators);

            
            c.ConnectionDisconnected += cDisconnected;
            c.MessageHandler = messageReadHandler;

            if (validators.Length == 0)
            {
                expectedMessage = NegotiationMessages.Success;
                if (position == ConnectionNegotiationPosition.Authorizer) writeNegMessage(NegotiationMessages.Success);
            }
            else
            {
                expectedMessage = position == ConnectionNegotiationPosition.Authorizer ? NegotiationMessages.Answer : NegotiationMessages.Challenge;
                if (position == ConnectionNegotiationPosition.Authorizer) writeNegMessage(NegotiationMessages.Challenge, validatorQueue.Peek().GetChallengeBytes());
            }
        }

        public ValidationSuccessfulHandler ValidationSuccessful;
        public ValidationFailedHandler ValidationFailed;

        public readonly Connection Connection;
        public readonly ConnectionNegotiationPosition Position;
        public readonly IReadOnlyList<ConnectionValidator> Validators;

        private readonly Queue<ConnectionValidator> validatorQueue;

        private void validationFailed(ValidationFailedException ex, bool send = true)
        {
            if (ValidationFinished) return;
            ValidationFinished = true;
            Connection.ConnectionDisconnected -= cDisconnected;
            if (send) Connection.WriteMessage(new byte[2] { (byte)NegotiationMessages.VerificationAborted, (byte)ex.FailureType });
            ValidationFailed.Invoke(this, ex);
            Connection.CloseConnection();
        }

        private void validationSucceeded()
        {
            if (ValidationFinished) return;
            ValidationFinished = true;
            Connection.ConnectionDisconnected -= cDisconnected;
            Connection.MessageHandler = null;
            ValidationSuccessful.Invoke(this);
        }

        private void writeNegMessage(NegotiationMessages m, byte[]? msg = null)
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

        private NegotiationMessages expectedMessage;
        public bool IsAborted { get; private set; } = false;

        private void messageReadHandler(ReadOnlySpan<byte> message, MessageArgs args)
        {
            NegotiationMessages mType = ((NegotiationMessages)message[0]);
            bool unexpected = mType != expectedMessage;
            var v = validatorQueue.Count == 0 ? null : validatorQueue.Dequeue();

            switch (mType)
            {
                case NegotiationMessages.VerificationAborted:
                    ValidationFailure f = ValidationFailure.Unknown;
                    if (message.Length > 2) f = (ValidationFailure)message[1];
                    validationFailed(new(f), false);
                    IsAborted = true;
                    return;
                case NegotiationMessages.Challenge:
                    if (v == null) validationFailed(new(ValidationFailure.TooManyValidators));
                    else if (unexpected) validationFailed(new(ValidationFailure.InvalidOrder));
                    else if (!v.ValidateChallenge(message.Slice(1))) validationFailed(new(ValidationFailure.InvalidChallenge)); 
                    else { writeNegMessage(NegotiationMessages.Answer, v.GetAnswerBytes()); expectedMessage = validatorQueue.Count == 0 ? NegotiationMessages.Success : NegotiationMessages.Challenge; }
                    return;
                case NegotiationMessages.Answer:
                    if (unexpected || v == null) validationFailed(new(ValidationFailure.InvalidOrder));
                    else if (!v.ValidateAnswer(message.Slice(1))) validationFailed(new(ValidationFailure.WrongAnswer));
                    else if (validatorQueue.Count == 0) { writeNegMessage(NegotiationMessages.Success); expectedMessage = NegotiationMessages.Success;  }
                    else writeNegMessage(NegotiationMessages.Challenge, validatorQueue.Peek().GetChallengeBytes());
                    return;
                case NegotiationMessages.Success:
                    if (unexpected || v != null) validationFailed(new(ValidationFailure.InsufficientValidators));
                    else {
                        if (Position == ConnectionNegotiationPosition.Challenger) writeNegMessage(NegotiationMessages.Success);
                        validationSucceeded();
                    }
                    return;
                default:
                    validationFailed(new(ValidationFailure.UnknownValidationMessage));
                    break;
            }
        }

        public static Connection ValidateConnection(Connection c, ConnectionNegotiationPosition pos, params ConnectionValidator[] validators)
        {
            if (c.MessageHandler != null) throw new InvalidOperationException("The negotiation can only work if no message handler has been set on the connection, since otherwise messages might already have been lost!");

            TaskCompletionSource<(bool, ValidationFailedException?)> tcs = new();

            ConnectionNegotiation n = new ConnectionNegotiation(c, pos, validators ?? new ConnectionValidator[0], (c, f) => { tcs.SetResult((false, f)); }, (c) => { tcs.SetResult((true, null)); });

            var res = tcs.Task.GetAwaiter().GetResult();
            if (!res.Item1) throw res.Item2 ?? new(ValidationFailure.Unknown);
            else return c;
        }

        public static async Task<Connection> ValidateConnectionAsync(Connection c, ConnectionNegotiationPosition pos, params ConnectionValidator[] validators)
        {
            return await Task.Run<Connection>(() =>
            {
                return ValidateConnection(c, pos, validators);
            });
        }

        public void AbortNegotiation()
        {
            writeNegMessage(NegotiationMessages.VerificationAborted);
            Connection.Dispose();
            validationFailed(new ValidationFailedException(ValidationFailure.Disconnect), false);
        }

        public bool ValidationFinished { get; private set; } = false;

        

        public void Dispose()
        {
            AbortNegotiation();
            GC.SuppressFinalize(this);
        }
    }

    public enum ConnectionNegotiationPosition
    {
        Challenger,
        Authorizer
    }

    public enum NegotiationMessages
    {
        VerificationAborted,
        Success,
        Challenge,
        Answer
    }
}
