using System.Collections.Frozen;
using Loris.Extensions;

namespace Loris.MQTT.Client
{
    /// <summary>
    /// Chirpstack integration event message handlers as described on:<br/>
    /// <see href="https://www.chirpstack.io/docs/chirpstack/integrations/events.html">Integration events</see>
    /// </summary>
    public interface IChirpstackIntegrationHandler
    {
        IReadOnlyList<Func<string, CancellationToken, Task>> Subscriptions { get; }

        /// <summary>
        /// <para>
        /// Creates MQTT routing table
        /// </para>
        /// <para>
        /// Maps MQTT event type to event handler. It is using Subscriptions<br/>
        /// property populated with methods specified by the interface.<br/>
        /// Each method being a dictionary key relates to the chirpstack integration event type.
        /// </para>
        /// </summary>
        /// <returns>
        /// It returns <b>lookup only</b> dictionary mapping
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Throws when the Subscriptions is empty
        /// </exception>
        public sealed FrozenDictionary<
            string,
            Func<string, CancellationToken, Task>
        > CreateRoutingTable()
        {
            if (Subscriptions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(Subscriptions)} is empty. Implementation should define at least one handler method"
                );
            }
            return Subscriptions.ToFrozenDictionary((m) => m.Method.Name.ToLower());
        }

        public Task Up(string jsonPayload, CancellationToken stoppingToken) => Task.CompletedTask;
        public Task Status(string jsonPayload, CancellationToken stoppingToken) =>
            Task.CompletedTask;
        public Task Join(string jsonPayload, CancellationToken stoppingToken) => Task.CompletedTask;
        public Task Ack(string jsonPayload, CancellationToken stoppingToken) => Task.CompletedTask;
        public Task TxAck(string jsonPayload, CancellationToken stoppingToken) =>
            Task.CompletedTask;
        public Task Log(string jsonPayload, CancellationToken stoppingToken) => Task.CompletedTask;
        public Task Location(string jsonPayload, CancellationToken stoppingToken) =>
            Task.CompletedTask;
    }
}
