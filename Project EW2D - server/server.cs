using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Server.Components;
using Stormancer.Diagnostics;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    static class GameSceneExtensions
    {
        public static void AddGameScene(this IAppBuilder builder)
        {
            builder.SceneTemplate("server", scene => new server(scene));
        }
    }

    class server
    {
        private ISceneHost _scene;
        private IEnvironment _env;
        private bool _isRunning = false;
        private uint _ids = 0;
        private ConcurrentDictionary<long, Player> _players = new ConcurrentDictionary<long, Player>();
        private ConcurrentDictionary<uint, Bullet> _bullets = new ConcurrentDictionary<uint, Bullet>(); 

        public  server(ISceneHost scene)
        {
            _scene = scene;
            _scene.GetComponent<ILogger>().Debug("server", "starting configuration");
            _env = _scene.GetComponent<IEnvironment>();
            _scene.Connecting.Add(onConnecting);
            _scene.Connected.Add(onConnected);
            _scene.Disconnected.Add(onDisconnected);

            _scene.AddRoute("update_position", onUpdatePosition);
            _scene.AddRoute("chat", onReceivingMessage);
 //           _scene.AddProcedure("firing_weapon", onFiringWeapon);
 //           _scene.AddProcedure("colliding", onColliding);

            _scene.Starting.Add(onStarting);
            _scene.Shuttingdown.Add(onShutdown);
            _scene.GetComponent<ILogger>().Debug("server", "configuration complete");

        }

        private Task _gameLoop;
        private Task onStarting(dynamic arg)
        {
            _scene.GetComponent<ILogger>().Debug("server", "starting game loop");
            _gameLoop = runGame();
            return Task.FromResult(true);
        }

        private async Task onShutdown(ShutdownArgs arg)
        {
            _scene.GetComponent<ILogger>().Debug("main", "the scene shuts down");
            _isRunning = false;
            try
            {
                await _gameLoop;

            }
            catch (Exception e)
            {
                _scene.GetComponent<ILogger>().Log(LogLevel.Error, "runtimeError", "an error occurred in the game loop", e);
            }
        }

        private Task onConnecting(IScenePeerClient client)
        {
            _scene.GetComponent<ILogger>().Debug("server", "un client tente de se connecter");
            if (_isRunning == false)
                throw new ClientException("le serveur est vérouillé.");
            else if (_players.Count >= 100)
                throw new ClientException("le serveur est complet.");
            return Task.FromResult(true);
        }

        private Task onConnected(IScenePeerClient client)
        {
            myGameObject player = client.GetUserData<myGameObject>();
            if (_players.Count < 100)
            {
                _scene.Broadcast("chat", player.name + " a rejoint le combat !");
                _scene.GetComponent<ILogger>().Debug("server", "client connected with name : " + player.name);
                client.Send("get_id", s =>
                {
                    var writer = new BinaryWriter(s, Encoding.UTF8, true);
                    writer.Write(client.Id);
                }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
                player.id = client.Id;
                sendConnectedPlayersToNewPeer(client);
                sendConnexionNotification(player);
                _players.TryAdd(client.Id, new Player(player, _env.Clock));
            }
            return Task.FromResult(true);
        }

        private void sendConnectedPlayersToNewPeer(IScenePeerClient client)
        {

            int i = 0;

            client.Send("player_connected", s =>
            {
                var writer = new BinaryWriter(s, Encoding.UTF8, true);
                foreach (Player p in _players.Values)
                {
                    writer.Write(p.id);
                    if (p.status == StatusTypes.ALIVE)
                        writer.Write(0);
                    else
                        writer.Write(1);
                    writer.Write(p.color_red);
                    writer.Write(p.color_blue);
                    writer.Write(p.color_green);
                    i++;
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);

            _scene.GetComponent<ILogger>().Debug("test", "sent " + i + " player data to newly connected player");
        }

        private void sendConnexionNotification(myGameObject p)
        {
            _scene.Broadcast("player_connected", s =>
            {
                var writer = new BinaryWriter(s, Encoding.UTF8, true);
                writer.Write(p.id);
                writer.Write(p.color_red);
                writer.Write(p.color_blue);
                writer.Write(p.color_green);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
        }

        private Task onDisconnected(DisconnectedArgs arg)
        {
            Player temp;
            _players.TryGetValue(arg.Peer.Id, out temp);
            _scene.Broadcast("chat", temp.name + " a quitté le combat !");
            _scene.Broadcast("Player_disconnected", s =>
            {
                var writer = new BinaryWriter(s, Encoding.UTF8, true);
                writer.Write(temp.id);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
            _players.TryRemove(temp.id, out temp);
            return Task.FromResult(true);
        }

        private void onReceivingMessage(Packet<IScenePeerClient> packet)
        {
            _scene.Broadcast("chat", packet.Stream);
        }

        private void onUpdatePosition(Packet<IScenePeerClient> packet)
        {
            var reader = new BinaryReader(packet.Stream);
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var rot = reader.ReadSingle();

            if (_players.ContainsKey(packet.Connection.Id))
                _players[packet.Connection.Id].updatePosition(x, y, rot, _env.Clock);
        }

        private async Task runGame()
        {
            _isRunning = true;
            long lastUpdate = _env.Clock;
            _scene.GetComponent<ILogger>().Debug("server", "starting game loop");
            while (_isRunning)
            {
                if (_env.Clock - lastUpdate > 100 && _players.Count > 0)
                {
                    lastUpdate = _env.Clock;
                    _scene.Broadcast("update_position", s =>
                    {
                        var writer = new BinaryWriter(s, Encoding.UTF8, true);
                        foreach (Player p in _players.Values)
                        {
                            if (p.lastUpdate < lastUpdate)
                            {
                                writer.Write(p.id);
                                writer.Write(p.pos_x);
                                writer.Write(p.pos_y);
                                writer.Write(p.rotation);
                                writer.Write(p.vect_x);
                                writer.Write(p.vect_y);
                            }
                        }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED);
                    _scene.Broadcast("update_status", s =>
                    {
                        var writer = new BinaryWriter(s, Encoding.UTF8, true);
                        foreach (Player p in _players.Values)
                        {
                            if (p.status == StatusTypes.ALIVE && p.life <= 0)
                            {
                                p.status = StatusTypes.DEAD;
                                writer.Write(p.id);
                                writer.Write(1); //StatusTypes.DEAD
                            }
                            else if (p.status == StatusTypes.DEAD && lastUpdate > p.lastHit + 5000)
                            {
                                p.status = StatusTypes.ALIVE;
                                p.life = 100;
                                writer.Write(p.id);
                                writer.Write(0); //StatusTypes.ALIVE
                            }
                        }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
                }
                await Task.Delay(100);
            }
        }
    } 
}
