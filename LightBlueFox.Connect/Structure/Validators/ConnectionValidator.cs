namespace LightBlueFox.Connect.Structure.Validators
{
    /// <summary>
    /// Connection validators ensure that the application on the other end of the connection is compatible and of the expected type.
    /// This is done through a simple answer/response scheme.
    /// it is important to note that most of these implementations offer no real security nor are they meant to do so. In order to address security concerns, 
    /// use encryption and certificates.
    /// </summary>
    public abstract class ConnectionValidator
    {
        /// <summary>
        /// Get the challenge in byte form.
        /// </summary>
        public abstract byte[] GetChallengeBytes();

        /// <summary>
        /// Get the answer in byte form.
        /// </summary>
        public abstract byte[] GetAnswerBytes();

        /// <summary>
        /// Check whether the challenge is as expected.
        /// </summary>
        public abstract bool ValidateChallenge(ReadOnlySpan<byte> challenge);
        
        /// <summary>
        /// Check whether the answer is as expected.
        /// </summary>
        public abstract bool ValidateAnswer(ReadOnlySpan<byte> answer);
    }
}
