// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Pipeline.Filters
{
    using System;
    using System.Threading.Tasks;
    using Util;


    /// <summary>
    /// Converts a ConsumeContext to a message type and passes the context to 
    /// the output pipe. Supports interception by message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public class MessageConsumeFilter<TMessage> :
        IFilter<ConsumeContext>,
        IConsumePipeConnector<TMessage>,
        IRequestPipeConnector<TMessage>,
        IConsumeMessageObserverConnector
        where TMessage : class
    {
        readonly MessageObserverConnectable<TMessage> _messageObservers;
        readonly TeeConsumeFilter<TMessage> _output;

        public MessageConsumeFilter()
        {
            _output = new TeeConsumeFilter<TMessage>();
            _messageObservers = new MessageObserverConnectable<TMessage>();
        }

        ConnectHandle IConsumePipeConnector<TMessage>.ConnectConsumePipe(IPipe<ConsumeContext<TMessage>> pipe)
        {
            return _output.ConnectConsumePipe(pipe);
        }

        async Task IFilter<ConsumeContext>.Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            ConsumeContext<TMessage> consumeContext;
            if (context.TryGetMessage(out consumeContext))
            {
                if (_messageObservers.Count > 0)
                    await _messageObservers.PreConsume(consumeContext);
                try
                {
                    await _output.Send(consumeContext, next);

                    if (_messageObservers.Count > 0)
                        await _messageObservers.PostConsume(consumeContext);
                }
                catch (Exception ex)
                {
                    // we can't await in a catch block, so we have to wait explicitly on this one
                    if (_messageObservers.Count > 0)
                    {
                        _messageObservers.ConsumeFault(consumeContext, ex)
                            .Wait(context.CancellationToken);
                    }

                    throw;
                }
            }
        }

        bool IFilter<ConsumeContext>.Visit(IPipelineVisitor visitor)
        {
            return visitor.Visit(this, x => _output.Visit(x));
        }

        ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        {
            var self = _messageObservers as MessageObserverConnectable<T>;
            if (self == null)
                throw new InvalidOperationException("The connection type is invalid: " + TypeMetadataCache<T>.ShortName);

            return self.Connect(observer);
        }

        ConnectHandle IRequestPipeConnector<TMessage>.ConnectRequestPipe(Guid requestId, IPipe<ConsumeContext<TMessage>> pipe)
        {
            return _output.ConnectRequestPipe(requestId, pipe);
        }
    }
}