using System;
using System.Threading.Tasks;

namespace IA.Events
{
    public delegate void ContinuousProcessEvent();

    public class ContinuousEvent : Event
    {
        /// <summary>
        /// Defines the time it takes until it gets called again.
        /// </summary>
        public int tickDelay = 0;

        /// <summary>
        /// processes the command, will tick every {tickDelay} seconds
        /// </summary>
        public ContinuousProcessEvent processEvent = () =>
        {
            Log.Message("Tick!");
        };

        public ContinuousEvent()
        {
            //  CommandUsed = 0;
        }

        public async Task Check()
        {
            if (TryProcessCommand())
            {
                //       CommandUsed++;
                await Task.Delay(tickDelay * 1000);
                await Check();
            }
        }

        public bool TryProcessCommand()
        {
            try
            {
                processEvent();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            return false;
        }
    }
}