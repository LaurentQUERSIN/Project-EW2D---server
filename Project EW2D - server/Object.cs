using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    public abstract class myGameObject
    {
        protected uint _id;
        protected string _name;
        protected float _pos_x;
        protected float _pos_y;
        protected float _rot;
        protected float _vect_x;
        protected float _vect_y;
        protected long _lastUpdate;

        public uint id { get { return _id; } }
        public string name { get { return _name; }  }
        public float pos_x {  get { return _pos_x; }  }
        public float pos_y  {  get { return _pos_y; }  }
        public float rot  {  get { return _rot; }  }
        public float vect_x { get { return _vect_x; } }
        public float vect_y { get { return _vect_y; } }
        public long lastUpdate { get { return _lastUpdate; } }

    }

    public class MovingObject : myGameObject
    {
        protected float _colorR;
        protected float _colorB;
        protected float _colorG;

        public float colorR { get { return _colorR; } }
        public float colorB { get { return _colorB; } }
        public float colorG { get { return _colorG; } }

        public Weapon weapon;
    }

    public class Player : MovingObject
    {
        public float lastx;
        public float lasty;
        public int life = 100;
        public int lastFired = 0;
        public int lastHit = 0;
        public StatusTypes status;


        public void updatePosition(float x, float y, float rot, long updateTime)
        {
            _vect_x = x - lastx;
            _vect_y = y - lasty;
            lastx = _pos_x;
            lasty = _pos_y;
            _pos_x = x;
            _pos_y = y;
            _rot = rot;

            _lastUpdate = updateTime;
        }

        public Player(uint pId, string pName, long updateTime)
        {
            _id = pId;
            _name = pName;
            _pos_x = 0;
            _pos_y = 0;
            lastx =0 ;
            lasty = 0;
            _rot = 0;
            _vect_x = 0;
            _vect_y = 0;
            _colorR = 255;
            _colorB = 255;
            _colorG = 255;
            weapon = Weapons.instance.getWeapon(WeaponTypes.STANDARD);
            _lastUpdate = updateTime;
            status = StatusTypes.ALIVE;

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
        private void calculateBulletCoords(Player player)
        {
            _vect_y = (float)Math.Sin((double)player.rot) * weapon.speed;
            _vect_x = (float)Math.Cos((double)player.rot) * weapon.speed;
            _pos_x = player.pos_x + _vect_x;
            _pos_y = player.pos_y + _vect_x;
        }

        public Bullet(uint object_id, Player player, long updateTime)
        {
            _id = object_id;
            _name = "";
            _rot = player.rot;
            _colorR = player.colorR;
            _colorB = player.colorB;
            _colorG = player.colorG;
            weapon = player.weapon;
            calculateBulletCoords(player);
            _lastUpdate = updateTime;
        }
    }
}
