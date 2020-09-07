using System.Threading.Tasks;

namespace Producer
{
    public interface IOrderEventsPublisher
    {
        void Publish(int orderId, string eventDescription);

    }
}