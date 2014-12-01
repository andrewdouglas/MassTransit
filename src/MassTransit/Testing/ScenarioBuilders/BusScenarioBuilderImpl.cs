﻿// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Testing.ScenarioBuilders
{
    using System;
    using MassTransit.Configurators;
    using Scenarios;


    /// <summary>
    /// Implementation for the test scenario, but abstract for others to customize it. Sets some defaults in the c'tor, which you
    /// can override with the <see cref="ConfigureBus"/> and <see cref="ConfigureSubscriptions"/> methods.
    /// </summary>
    public class BusScenarioBuilderImpl :
        EndpointScenarioBuilderImpl<BusTestScenario>,
        BusScenarioBuilder
    {
        readonly InMemoryBusFactoryConfigurator _configurator;

        /// <summary>
        /// c'tor
        /// </summary>
        public BusScenarioBuilderImpl()
        {
            _configurator = new InMemoryBusFactoryConfigurator();
        }

        public void ConfigureBus(Action<IInMemoryServiceBusFactoryConfigurator> configureCallback)
        {
            configureCallback(_configurator);
        }

        public void ConfigureSubscriptions(Action<IReceiveEndpointConfigurator> configureCallback)
        {
            _configurator.ReceiveEndpoint("input_queue", configureCallback);
        }

        public override BusTestScenario Build()
        {
            var scenario = new BusTestScenarioImpl();

            scenario.Bus = _configurator.Build();

            return scenario;
        }
    }
}