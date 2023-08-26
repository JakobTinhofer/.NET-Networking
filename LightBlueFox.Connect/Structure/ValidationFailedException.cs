using System.Runtime.Serialization;

namespace LightBlueFox.Connect.Structure
{
    public class ValidationFailedException : Exception
    {
        public readonly ValidationFailure FailureType;
        public ValidationFailedException(ValidationFailure type) : base("The validation negotiation failed. Failure reason: " + type)
        {
            FailureType = type;
        }
    }

    public enum ValidationFailure
    {
        Unknown,
        WrongAnswer,
        InvalidChallenge,
        TooManyValidators,
        InsufficientValidators,
        UnknownValidationMessage,
        InvalidOrder,
        Disconnect
    }
}
