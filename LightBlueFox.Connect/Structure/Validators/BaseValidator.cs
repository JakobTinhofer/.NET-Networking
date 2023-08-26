namespace LightBlueFox.Connect.Structure.Validators
{
    public abstract class BaseValidator
    {
        public abstract byte[] GetChallengeBytes();

        public abstract byte[] GetAnswerBytes();

        public abstract bool ValidateChallenge(ReadOnlySpan<byte> challenge);
        public abstract bool ValidateAnswer(ReadOnlySpan<byte> answer);
    }
}
