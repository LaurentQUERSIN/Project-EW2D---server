using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    public abstract class Object
    {
        protected uint _id;
        protected string _name;
        protected float _pos_x;
        protected float _pos_y;
        protected char _rot;
        protected float _vect_x;
        protected float _vect_y;
        protected long _lastUpdate;

        public uint id { get { return _id; } }
        public string name { get { return _name; }  }
        public float pos_x {  get { return _pos_x; }  }
        public float pos_y  {  get { return _pos_y; }  }
        public char rot  {  get { return _rot; }  }
        public float vect_x { get { return _vect_x; } }
        public float vect_y { get { return _vect_y; } }
        public long lastUpdate { get { return _lastUpdate; } }

    }

    public class MovingObject : Object
    {
        protected char _colorR;
        protected char _colorB;
        protected char _colorG;

        public char colorR { get { return _colorR; } }
        public char colorB { get { return _colorB; } }
        public char colorG { get { return _colorG; } }

        public Weapon weapon;
    }

    public class Player : MovingObject
    {
        public int life;
        public int shield;
        public int lastFired;
        public int lastHit;

        public void updatePosition(Object player)
        {
            _pos_x = player.pos_x;
            _pos_y = player.pos_y;
            _rot = player.rot;
            _vect_x = player.vect_x;
            _vect_y = player.vect_y;
        }

        public void updatePosition(float x, float y, char rot, float vx, float vy, long updateTime)
        {
            _pos_x = x;
            _pos_y = y;
            _rot = rot;
            _vect_x = vx;
            _vect_y = vy;
            _lastUpdate = updateTime;
        }

        public Player(PlayerInfo player, long updateTime)
        {
            _id = player.id;
            _name = player.name;
            _pos_x = player.pos_x;
            _pos_y = player.pos_y;
            _rot = player.rot;
            _vect_x = 0;
            _vect_y = 0;
            _colorR = player.colorR;
            _colorB = player.colorB;
            _colorG = player.colorG;
            weapon = Weapons.instance.getWeapon(WeaponTypes.STANDARD);
            _lastUpdate = updateTime;
        }
    }

    public class PlayerInfo : MovingObject
    {
        internal static PlayerInfo FromPeer(IScenePeerClient peer)
        {
            return peer.GetUserData<PlayerInfo>();
        }

        public void setId(uint new_id)
        {
            _id = new_id;
        }
    }

    public class Bullet : MovingObject
    {
        private DateTime _spawntime = DateTime.UtcNow;
        public DateTime spawntime { get { return _spawntime; } }

        private void calculateBulletCoords(Player player)
        {
            _vect_y = (float)Math.Sin((double)player.rot) * weapon.speed;
            _vect_x = (float)Math.Cos((double)player.rot) * weapon.speed;
            _pos_x = player.pos_x + _vect_x;
            _pos_y = player.pos_y + _vect_x;
        }

        public Bullet(uint object_id, Player player)
        {
            _id = object_id;
            _name = "";
            _rot = player.rot;
            _colorR = player.colorR;
            _colorB = player.colorB;
            _colorG = player.colorG;
            weapon = player.weapon;
            calculateBulletCoords(player);
        }
    }
}
