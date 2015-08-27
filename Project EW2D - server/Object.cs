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
        protected uint _id = 0;
        protected string _name = "";
        protected float _pos_x = 0;
        protected float _pos_y = 0;
        protected float _rot = 0;
        protected long _lastUpdate = 0;

        public float color_red = 1;
        public float color_blue = 1;
        public float color_green = 1;

        public uint id { get { return _id; } }
        public string name { get { return _name; }  }
        public float pos_x {  get { return _pos_x; }  }
        public float pos_y  {  get { return _pos_y; }  }
        public float rot  {  get { return _rot; }  }
        public long lastUpdate { get { return _lastUpdate; } }

        public void setId(uint new_id)
        {
            _id = new_id;
        }
    }

    public class MovingObject : myGameObject
    {
        protected float _vect_x = 0;
        protected float _vect_y = 0;
        protected float _last_x = 0;
        protected float _last_y = 0;
        public float vect_x { get { return _vect_x; } }
        public float vect_y { get { return _vect_y; } }
        public float last_x { get { return _last_x; } }
        public float last_y { get { return _last_y; } }

        public void updatePosition(float x, float y, float rot, long updateTime)
        {
            _vect_x = x - _last_x;
            _vect_y = y - _last_y;
            _last_x = _pos_x;
            _last_y = _pos_y;
            _pos_x = x;
            _pos_y = y;
            _rot = rot;
            _lastUpdate = updateTime;
        }
    }

    public class Player : MovingObject
    {
        public int life = 100;
        public int lastFired = 0;
        public int lastHit = 0;
        public StatusTypes status = StatusTypes.ALIVE;
        public Weapon weapon = null;

        public Player(uint pId, string pName, long updateTime)
        {
            _id = pId;
            _name = pName;
            weapon = Weapons.instance.getWeapon(WeaponTypes.STANDARD);
            _lastUpdate = updateTime;
        }
        public Player(myGameObject obj, long updateTime)
        {
            _id = obj.id;
            _name = obj.name;
            _pos_x = obj.pos_x;
            _pos_y = obj.pos_y;
            _rot = obj.rot;
            color_red = obj.color_red;
            color_blue = obj.color_blue;
            color_green = obj.color_green;
            _lastUpdate = updateTime;
        }
    }

    public class Bullet : MovingObject
    {
        public Weapon weapon = null;

        private void calculateBulletCoords(Player player)
        {
            _vect_y = (float)Math.Sin((double)rot) * weapon.speed;
            _vect_x = (float)Math.Cos((double)rot) * weapon.speed;
            _pos_x = player.pos_x + _vect_x * (weapon.speed * ((player.lastUpdate - _lastUpdate) / 1000f));
            _pos_y = player.pos_y + _vect_y * (weapon.speed * ((player.lastUpdate - _lastUpdate) / 1000f));
        }

        public bool isColliding(Player p, long time)
        {
            float x = _pos_x + (_vect_x * (weapon.speed * ((time - _lastUpdate) / 1000f)));
            float y = _pos_y + (_vect_y * (weapon.speed * ((time - _lastUpdate) / 1000f)));
            if (p.pos_x - 100 <= x && x <= p.pos_x + 100 && p.pos_y - 100 <= y && y <= p.pos_y + 100)
                return true;
            return false;
        }

        public Bullet(uint object_id, Player player, long updateTime)
        {
            _id = object_id;
            _name = player.name + " bullet " + _id.ToString();
            _rot = player.rot;
            color_red = player.color_red;
            color_blue = player.color_blue;
            color_green = player.color_green;
            weapon = player.weapon;
            calculateBulletCoords(player);
            _lastUpdate = updateTime;
        }
    }
}
