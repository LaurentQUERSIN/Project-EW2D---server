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
        public uint id = 0;
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

        public void updatePosition(float x, float y, float rot, long updateTime)
        {
            vect_x = x - last_x;
            vect_y = y - last_y;
            last_x = pos_x;
            last_y = pos_y;
            pos_x = x;
            pos_y = y;
            rotation = rot;
            lastUpdate = updateTime;
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
            id = pId;
            name = pName;
            weapon = Weapons.instance.getWeapon(WeaponTypes.STANDARD);
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

        private void calculateBulletCoords(Player player)
        {
            vect_y = (float)Math.Sin((double)rotation) * weapon.speed;
            vect_x = (float)Math.Cos((double)rotation) * weapon.speed;
            pos_x = player.pos_x + vect_x * (weapon.speed * ((player.lastUpdate - lastUpdate) / 1000f));
            pos_y = player.pos_y + vect_y * (weapon.speed * ((player.lastUpdate - lastUpdate) / 1000f));
        }

        public bool isColliding(Player p, long time)
        {
            float x = pos_x + (vect_x * (weapon.speed * ((time - lastUpdate) / 1000f)));
            float y = pos_y + (vect_y * (weapon.speed * ((time - lastUpdate) / 1000f)));
            if (p.pos_x - 100 <= x && x <= p.pos_x + 100 && p.pos_y - 100 <= y && y <= p.pos_y + 100)
                return true;
            return false;
        }

        public Bullet(uint object_id, Player player, long updateTime)
        {
            id = object_id;
            name = player.name + " bullet " + id.ToString();
            rotation = player.rotation;
            color_red = player.color_red;
            color_blue = player.color_blue;
            color_green = player.color_green;
            weapon = player.weapon;
            calculateBulletCoords(player);
            lastUpdate = updateTime;
        }
    }
}
