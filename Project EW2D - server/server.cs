﻿using Stormancer;
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

            _scene.AddRoute("enable_action", OnEnableAction);
            _scene.AddRoute("disable_action", OnDisableAction);

            _scene.AddRoute("firing_weapon", onFiringWeapon);
            _scene.AddRoute("chat", onReceivingMessage);

            _scene.Starting.Add(onStarting);
            _scene.Shuttingdown.Add(onShutdown);
            _scene.GetComponent<ILogger>().Debug("server", "configuration complete");

        }

        private Task _gameLoop;
        private Task onStarting(dynamic arg)
        {
            Weapons.instance.setScene(_scene);
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
                }
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
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
            _scene.Broadcast("player_disconnected", s =>
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

        private void onFiringWeapon(Packet<IScenePeerClient> packet)
        {
            var reader = new BinaryReader(packet.Stream);
            var id = reader.ReadInt64();
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var time = reader.ReadInt64();

            if (_players.ContainsKey(id) &&
                _players[id].lastFired + _players[id].weapon.cooldown < _env.Clock)
            {
                _players[id].lastFired = _env.Clock;
                CreatePlayerBullet(_players[id], x, y, time);
            }
        }

        private async void CreatePlayerBullet(Player p, float x, float y, long time)
        {
           await p.weapon.Fire(p, x, y, time);
        }

        private void OnEnableAction(Packet<IScenePeerClient> packet)
        {
            var reader = new BinaryReader(packet.Stream);
            var action = reader.ReadInt32();

            Player p;
            if (_players.TryGetValue(packet.Connection.Id, out p) == true)
            {
                if (action == 0)
                    p.up = true;
                if (action == 1)
                    p.down = true;
                if (action == 2)
                    p.left = true;
                if (action == 3)
                    p.right = true;
                if (action == 4)
                    p.fire = true;
            }
        }

        private void OnDisableAction(Packet<IScenePeerClient> packet)
        {
            var reader = new BinaryReader(packet.Stream);
            var action = reader.ReadInt32();

            Player p;
            if (_players.TryGetValue(packet.Connection.Id, out p) == true && p.status == StatusTypes.ALIVE)
            {
                if (action == 0)
                    p.up = false;
                if (action == 1)
                    p.down = false;
                if (action == 2)
                    p.left = false;
                if (action == 3)
                    p.right = false;
                if (action == 4)
                    p.fire = false;
            }
        }

        private void updatePlayersPositions(long lastUpdate)
        {
            float deltaTime = (float)(_env.Clock - lastUpdate) / 1000f;
            foreach (Player p in _players.Values)
            {
                if (p.status == StatusTypes.ALIVE)
                {
                    if (p.up == true && p.down == false)
                    {
                        p.vect_y += 0.25f * deltaTime;
                    }
                    if (p.down == true && p.up == false)
                    {
                        p.vect_y -= 0.25f * deltaTime;
                    }
                    if ((p.up == false && p.down == false) || (p.up == true && p.down == true))
                    {
                        if (-0.05f < p.vect_y && p.vect_y < 0.05f)
                            p.vect_y = 0;
                        else if (p.vect_y > 0)
                            p.vect_y -= 0.1f * deltaTime;
                        else if (p.vect_y < 0)
                            p.vect_y += 0.1f * deltaTime;
                    }
                    if (p.vect_y > 0.5f)
                        p.vect_y = 0.5f;
                    if (p.vect_y < -0.5f)
                        p.vect_y = -0.5f;

                    if (p.left == true && p.right == false)
                    {
                        p.vect_x -= 0.25f * deltaTime;
                    }
                    if (p.right == true && p.left == false)
                    {
                        p.vect_x += 0.25f * deltaTime;
                    }
                    if ((p.left == false && p.right == false) || (p.left == true && p.right == true))
                    {
                        if (-0.05f < p.vect_x && p.vect_x < 0.05f)
                            p.vect_x = 0;
                        else if (p.vect_x > 0)
                            p.vect_x -= 0.1f * deltaTime;
                        else if (p.vect_x < 0)
                            p.vect_x += 0.1f * deltaTime;
                    }
                    if (p.vect_x > 0.5f)
                        p.vect_x = 0.5f;
                    if (p.vect_x < -0.5f)
                        p.vect_x = -0.5f;

                    p.pos_x += p.vect_x * deltaTime;
                    p.pos_y += p.vect_y * deltaTime;
                    if (p.pos_x < -150)
                    {
                        p.pos_x = -150;
                        p.vect_x = 0;
                    }
                    if (p.pos_x > 150)
                    {
                        p.pos_x = 150;
                        p.vect_x = 0;
                    }
                    if (p.pos_y < -100)
                    {
                        p.pos_y = -100;
                        p.vect_y = 0;
                    }
                    if (p.pos_y > 100)
                    {
                        p.pos_y = 100;
                        p.vect_y = 0;
                    }
                    CheckIfBulletCollideWithPlayers(p);
                }
                else if (p.status == StatusTypes.DEAD)
                {
                    if (p.lastHit + 5000 < _env.Clock)
                    {
                        p.status = StatusTypes.ALIVE;
                        _scene.Broadcast("update_status", s =>
                        {
                            using (var w = new BinaryWriter(s, Encoding.UTF8, false))
                            {
                                w.Write(p.id);
                                w.Write(1);
                            }
                        }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
                    }
                }
            }
        }

        private void CheckIfBulletCollideWithPlayers(Player p)
        {
            foreach (Bullet b in _bullets.Values)
            {
                var bullet_x = b.pos_x + (b.vect_x * (_env.Clock - b.lastUpdate) / 1000f);
                var bullet_y = b.pos_y + (b.vect_y * (_env.Clock - b.lastUpdate) / 1000f);
                double x = Math.Pow(p.pos_x - (double)bullet_x, 2);
                double y = Math.Pow(p.pos_y - (double)bullet_y, 2);
                var dist = Math.Sqrt(x + y);
                if (dist < 0)
                    dist = -dist;
                if (dist < 5.0f)
                {
                    _scene.GetComponent<ILogger>().Debug("main", "A bullet hit a player !");
                    p.life -= b.weapon.damage;
                    if (p.life <= 0)
                    {
                        p.lastHit = _env.Clock;
                        p.status = StatusTypes.DEAD;
                        _scene.Broadcast("update_status", s =>
                        {
                            using (var w = new BinaryWriter(s, Encoding.UTF8, false))
                            {
                                w.Write(p.id);
                                w.Write(1);
                            }
                        }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
                    }
                }
            }
        }

        private async Task runGame()
        {
            _isRunning = true;
            long lastUpdate = _env.Clock;
            _scene.GetComponent<ILogger>().Debug("server", "starting game loop");
            while (_isRunning)
            {
                updatePlayersPositions(lastUpdate);
                if (lastUpdate + 100 < _env.Clock && _players.Count > 0)
                {
                    lastUpdate = _env.Clock;
                    _scene.Broadcast("update_position", s =>
                    {
                        var writer = new BinaryWriter(s, Encoding.UTF8, true);
                        foreach (Player p in _players.Values)
                        {
                            writer.Write(p.id);
                            writer.Write(p.pos_x);
                            writer.Write(p.pos_y);
                            writer.Write(p.vect_x);
                            writer.Write(p.vect_y);
                            writer.Write(_env.Clock);
                         }
                    }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED);
                    foreach (Bullet bullet in Weapons.instance.bullets.Values)
                    {
                        Bullet temp;
                        if (bullet.lastUpdate + 20000 < _env.Clock)
                        {
                            _scene.Broadcast("destroy_bullet", s =>
                            {
                                var writer = new BinaryWriter(s, Encoding.UTF8, false);
                                writer.Write(bullet.id);
                            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);

                            Weapons.instance.bullets.TryRemove(bullet.id, out temp);
                        }
                    }
                }
                await Task.Delay(50);
            }
        }
    } 
}
