namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    /// <summary>
    /// Thrown when multiple <see cref="CompositeLibraryEntry{T}"/>(s) are cyclically dependent on each other.
    /// Support for cyclically dependent types is currently not planned.
    /// </summary>
    public class CyclicDependencyException : Exception
    {
        public CyclicDependencyException(Type t1, Type t2) : base("Type " + t1 + " and " + t2 + " are dependent on each other. Cyclic dependencies are not supported at this moment.")
        {

        }
    }
}
