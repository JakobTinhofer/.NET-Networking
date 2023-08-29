using System.Runtime.Serialization;

namespace LightBlueFox.Connect.Structure
{
    /// <summary>
    /// Describes the reasons behind a failed validation negotiation.
    /// </summary>
    public class ValidationFailedException : Exception
    {
        /// <summary>
        /// The type of failure that occurred.
        /// </summary>
        public readonly ValidationFailure FailureType;

        /// <summary>
        /// Create a new validation failed exception with a given failure type.
        /// </summary>
        /// <param name="type">The type of failure that occurred.</param>
        public ValidationFailedException(ValidationFailure type) : base("The validation negotiation failed. Failure reason: " + type)
        {
            FailureType = type;
        }
    }

    /// <summary>
    /// Contains all the different kinds of failure that could occur when trying to validate a connection.
    /// </summary>
    public enum ValidationFailure
    {
        /// <summary>
        /// Other types not applicable/No reason could be determined.
        /// </summary>
        Unknown,
        /// <summary>
        /// The Challenger answered the authorizers' challenge incorrectly.
        /// </summary>
        WrongAnswer,
        /// <summary>
        /// The challenge by the authorizer was not valid according to the challenger.
        /// </summary>
        InvalidChallenge,
        /// <summary>
        /// The authorizer expected more validators than the challenger has.
        /// </summary>
        TooManyValidators,
        /// <summary>
        /// The authorizer has less validators set than the challenger.
        /// </summary>
        InsufficientValidators,
        /// <summary>
        /// The sent message was not one of the known <see cref="NegotiationMessages"/>.
        /// </summary>
        UnknownNegotiationMessage,
        /// <summary>
        /// The order in which the negotiation messages where sent was unexpected.
        /// </summary>
        InvalidOrder,
        /// <summary>
        /// The connection was terminated on either end.
        /// </summary>
        Disconnect
    }
}
