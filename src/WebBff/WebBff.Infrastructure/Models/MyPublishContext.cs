using MassTransit;

namespace WebBff.Infrastructure.Models;
public class MyPublishContext<T> : IPipe<PublishContext<T>> where T : class
{
    public void Probe(ProbeContext context) => throw new NotImplementedException();
    public Task Send(PublishContext<T> context) => throw new NotImplementedException();
}
