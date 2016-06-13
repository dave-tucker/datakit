using System;

namespace Datakit
{
    [Serializable]
    public class NoHeadException : Exception
    {
        public NoHeadException(string message) : base(message)
        {
        }
    }
}
