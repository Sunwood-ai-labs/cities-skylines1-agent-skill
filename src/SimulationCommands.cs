using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class SimulationCommands
    {
        public static CommandResult SetSimulationSpeed(string body)
        {
            bool paused = JsonUtil.GetBool(body, "paused", false);
            int speed = (int)JsonUtil.GetNumber(body, "speed", 1f);

            if (speed < 1)
            {
                speed = 1;
            }
            if (speed > 3)
            {
                speed = 3;
            }

            SimulationManager simulation = Singleton<SimulationManager>.instance;
            simulation.ForcedSimulationPaused = false;
            simulation.SelectedSimulationSpeed = speed;
            simulation.SimulationPaused = paused;

            return CommandResult.FromJson("{\"ok\":true,\"paused\":" + JsonUtil.Bool(simulation.SimulationPaused) +
                ",\"selectedSpeed\":" + simulation.SelectedSimulationSpeed +
                ",\"finalSpeed\":" + simulation.FinalSimulationSpeed + "}");
        }
    }
}
