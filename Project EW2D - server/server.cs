using Stormancer;
using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Server.Components;
using Stormancer.Diagnostics;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
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
        private Dictionary<uint, Player> _players = new Dictionary<uint, Player>();
        private Dictionary<uint, Bullet> _bullets = new Dictionary<uint, Bullet>(); 

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
 //         _scene.AddRoute("firing_weapon", onFiringWeapon);
 //         _scene.AddRoute("colliding", onColliding);
            _scene.Starting.Add(onStarting);
            _scene.Shuttingdown.Add(onShutdown);
            _scene.GetComponent<ILogger>().Debug("server", "configuration complete");

        }

        private Task onStarting(dynamic arg)
        {
            _scene.GetComponent<ILogger>().Debug("server", "starting game loop");
            runGame();
            return Task.FromResult(true);
        }

        private Task onShutdown(ShutdownArgs arg)
        {
            return Task.FromResult(true);
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
                    using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                        writer.Write(_ids);
                }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
                player.setId(_ids);
                sendConnectedPlayersToNewPeer(client);
                sendConnexionNotification(player);
                _players.Add(_ids, new Player(player, _env.Clock));
                _ids++;
            }
            return Task.FromResult(true);
        }

        private void sendConnectedPlayersToNewPeer(IScenePeerClient client)
        {
            client.Send("update_status", s =>
            {
                using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                {
                    foreach (Player p in _players.Values)
                    {
                        writer.Write(p.id);
                        writer.Write(2); // StatusTypes.CONNECTED
                        writer.Write(p.color_red);
                        writer.Write(p.color_blue);
                        writer.Write(p.color_green);
                    }
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
        }

        private void sendConnexionNotification(myGameObject p)
        {
            _scene.Broadcast("update_status", s =>
            {
                using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                {
                    writer.Write(p.id);
                    writer.Write(2); //StatusTypes.CONNECTED
                    writer.Write(p.color_red);
                    writer.Write(p.color_blue);
                    writer.Write(p.color_green);
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
        }

        private Task onDisconnected(DisconnectedArgs arg)
        {
            myGameObject player = arg.Peer.GetUserData<myGameObject>();
            _scene.Broadcast("chat", player.name + " a quitté le combat !");
            _scene.Broadcast("update_status", s =>
            {
                using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                {
                    writer.Write(player.id);
                    writer.Write(3); //StatusTypes.DISCONNECTED
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
            _players.Remove(player.id);
            return Task.FromResult(true);
        }

        private void onReceivingMessage(Packet<IScenePeerClient> packet)
        {
            _scene.Broadcast("chat", packet.Stream);
        }

        private void onUpdatePosition(Packet<IScenePeerClient> packet)
        {
            using (var reader = new BinaryReader(packet.Stream))
            {
                var id = reader.ReadUInt32();
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var rot = reader.ReadSingle();

                if (_players.ContainsKey(id))
                    _players[id].updatePosition(x, y, rot, _env.Clock);
                reader.Close();
            }
        }

        private void runGame()
        {
            _isRunning = true;
            long lastUpdate = _env.Clock;
            _scene.GetComponent<ILogger>().Debug("server", "starting game loop");
            while (_isRunning)
            {
                if (_env.Clock - lastUpdate > 100 && _players.Count > 0)
                {
                    _scene.GetComponent<ILogger>().Debug("server", "update position and status");
                    lastUpdate = _env.Clock;
                    _scene.Broadcast("update_position", s =>
                    {
                        using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                        {
                            foreach (Player p in _players.Values)
                            {
                                if (p.lastUpdate < lastUpdate)
                                {
                                    writer.Write(p.id);
                                    writer.Write(p.pos_x);
                                    writer.Write(p.pos_y);
                                    writer.Write(p.rot);
                                    writer.Write(p.vect_x);
                                    writer.Write(p.vect_y);
                                    writer.Write(p.lastUpdate);
                                }
                            }
                        }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED);
                    _scene.Broadcast("update_status", s =>
                    {
                        using (var writer = new BinaryWriter(s, Encoding.UTF8, false))
                        {
                            foreach (Player p in _players.Values)
                            {
                                if ( p.status == StatusTypes.ALIVE && p.life <= 0)
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
                        }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE_SEQUENCED);
                }
            }
        }
    }

    
}
