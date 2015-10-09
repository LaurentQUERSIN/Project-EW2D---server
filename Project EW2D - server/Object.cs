using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    public class myGameObject
    {
        public long id = 0;
        public string name = "";
        public float pos_x = 0;
        public float pos_y = 0;
        public float rotation = 0;
        public long lastUpdate = 0;

        public float color_red = 1;
        public float color_blue = 1;
        public float color_green = 1;

    }

    public class MovingObject : myGameObject
    {
        public float vect_x = 0;
        public float vect_y = 0;
        public float last_x = 0;
        public float last_y = 0;

        public void updatePosition(float x, float y, long updateTime)
        {
            vect_x = x - last_x;
            vect_y = y - last_y;
            last_x = pos_x;
            last_y = pos_y;
            pos_x = x;
            pos_y = y;
            lastUpdate = updateTime;
        }
    }

    public class Player : MovingObject
    {
        public int life = 100;
        public long lastFired = 0;
        public long lastHit = 0;
        public StatusTypes status = StatusTypes.ALIVE;
        public Weapon weapon = Weapons.instance.getWeapon(WeaponTypes.RAPID_FIRE);

        public bool up = false;
        public bool down = false;
        public bool left = false;
        public bool right = false;

        public Player(long pId, string pName, long updateTime)
        {
            id = pId;
            name = pName;
            weapon = Weapons.instance.getWeapon(WeaponTypes.RAPID_FIRE);
            lastUpdate = updateTime;
        }
        public Player(myGameObject obj, long updateTime)
        {
            id = obj.id;
            name = obj.name;
            pos_x = obj.pos_x;
            pos_y = obj.pos_y;
            rotation = obj.rotation;
            color_red = obj.color_red;
            color_blue = obj.color_blue;
            color_green = obj.color_green;
            lastUpdate = updateTime;
        }
    }

    public class Bullet : MovingObject
    {
        public Weapon weapon = null;

        public Bullet(long object_id, Player player, float x, float y, float vx, float vy, long updateTime)
        {
            id = object_id;
            pos_x = x;
            pos_y = y;
            vect_x = vx;
            vect_y = vy;
            weapon = player.weapon;
            lastUpdate = updateTime;
        }
    }
}
