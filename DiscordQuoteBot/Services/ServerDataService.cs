using DiscordQuoteBot.Models;
using Newtonsoft.Json;

namespace DiscordQuoteBot.Services
{
    public class ServerDataService
    {
        private const string DataFile = "savedData.json";

        private Dictionary<ulong, ServerData> _serverData = new();

        public ServerDataService()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var fileText = File.ReadAllText(DataFile);
                _serverData = JsonConvert.DeserializeObject<Dictionary<ulong, ServerData>>(fileText) ?? new Dictionary<ulong, ServerData>();
            }
            catch
            {
                _serverData = new Dictionary<ulong, ServerData>();
                SaveData();
            }
        }
        internal void SaveData() => File.WriteAllText(DataFile, JsonConvert.SerializeObject(_serverData));

        internal ServerData GetDataForServer(ulong serverId)
        {
            if (_serverData.TryGetValue(serverId, out var data))
            {
                return data;
            }

            data = new ServerData();
            _serverData.Add(serverId, data);
            return data;
        }
    }
}
