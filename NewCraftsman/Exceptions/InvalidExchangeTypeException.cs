﻿namespace NewCraftsman.Exceptions
{
    using System;

    [Serializable]
    public class InvalidExchangeTypeException : Exception
    {
        public InvalidExchangeTypeException() : base($"The given message broker was not recognized.")
        {
        }

        public InvalidExchangeTypeException(string broker) : base($"The message broker `{broker}` was not recognized.")
        {
        }
    }
}