﻿
using NServiceBus;

namespace UsageSample.Messaging
{
    public class TestCommand : ICommand
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
    }
}