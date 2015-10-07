using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Core;

namespace Project_EW2D___server
{
    public enum WeaponTypes
    {
        STANDARD,
        RAPID_FIRE,
        MACHINE_GUN,
        MEGA_BOMB
    }

    public class Weapons
    {
        public ISceneHost scene { get; set; }
        private Dictionary<WeaponTypes, Weapon> _weapons;

        public Weapon getWeapon(WeaponTypes wp)
        {
            return _weapons[wp];
        }

        private static Weapons _instance;
        public static Weapons instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Weapons();
                return _instance;
            }
        }

        private Weapons()
        {
            _weapons = new Dictionary<WeaponTypes, Weapon>();
            _weapons.Add(WeaponTypes.STANDARD, new WeaponStandard());
            _weapons.Add(WeaponTypes.RAPID_FIRE, new WeaponRapidFire());
            _weapons.Add(WeaponTypes.MACHINE_GUN, new WeaponMachineGun());
        }
    }

    public class Weapon
    {
        public long cooldown;
        public int damage;
        public double spread;
        public double speed;
        public float size;
        public string name;

        protected Random rand = new Random();

        protected void sendBullet(float bx, float by, float vx, float vy)
        {
            if (Weapons.instance.scene == null)
                return;
            Weapons.instance.scene.Broadcast("bullet", s =>
            {
                var writer = new BinaryWriter(s, Encoding.UTF8, false);
                writer.Write(bx);
                writer.Write(by);
                writer.Write(vx);
                writer.Write(vy);
                writer.Write(size);
            });
        }

        protected void CalcNextBullet(float px, float py, float bx, float by)
        {
            double vx = (double)(bx - px);
            double vy = (double)(by - py);

            Normalize(ref vx, ref vy);

            vx = vx + ((rand.NextDouble() - 1) % spread);
            vy = vy + ((rand.NextDouble() - 1) % spread);

            Normalize(ref vx, ref vy);

            vx = vx * speed;
            vy = vy * speed;

            bx = bx + (float)vx;
            by = by + (float)vy;

            sendBullet(bx, by, (float) vx, (float) vy);
        }

        protected void Normalize(ref double x, ref double y)
        {
            double length;

            length = Math.Sqrt((x * x) + (y * y));

            x = x / length;
            y = y / length;

        }

        virtual public Task Fire(float player_x, float player_y, float target_x, float target_y)
        {
            return Task.FromResult(true);
        }
    }
        public class WeaponStandard: Weapon
    {
        public WeaponStandard()
        {
            name = "standard gun";
            cooldown = 500;
            damage = 50;
            size = 10;
            speed = 5;
            spread = 0.1f;
        }

        public override Task Fire(float player_x, float player_y, float target_x, float target_y)
        {
            CalcNextBullet(player_x, player_y, target_x, target_y);
            return Task.FromResult(true);
        }
    }

    public class WeaponRapidFire : Weapon
    {
        public WeaponRapidFire()
        {  
            name = "rapid fire gun";
            cooldown = 1000;
            damage = 20;
            size = 5;
            speed = 15;
            spread = 0.05f;
        }

        public override Task Fire(float player_x, float player_y, float target_x, float target_y)
        {
            CalcNextBullet(player_x, player_y, target_x, target_y);
            Task.Delay(200);
            CalcNextBullet(player_x, player_y, target_x, target_y);
            Task.Delay(200);
            CalcNextBullet(player_x, player_y, target_x, target_y);

            return Task.FromResult(true);
        }
    }

    public class WeaponMachineGun : Weapon
    {
        public WeaponMachineGun()
        {
            name = "machine gun";
            cooldown = 100;
            damage = 10;
            size = 5;
            speed = 10;
            spread = 1;
        }

        public override Task Fire(float player_x, float player_y, float target_x, float target_y)
        {
            CalcNextBullet(player_x, player_y, target_x, target_y);
            return Task.FromResult(true);
        }
    }
}
