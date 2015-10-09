using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stormancer;
using Stormancer.Core;
using Stormancer.Server.Components;

namespace Project_EW2D___server
{
    public enum WeaponTypes
    {
        STANDARD,
        RAPID_FIRE,
        MACHINE_GUN,
        SHOTGUN
    }

    public class Weapons
    {
        public ISceneHost scene { get; set; }
        public Dictionary<WeaponTypes, Weapon> _weapons;
        public ConcurrentDictionary<long, Bullet> bullets = new ConcurrentDictionary<long, Bullet>();
        public long id = 0;

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
            _weapons.Add(WeaponTypes.SHOTGUN, new WeaponShotgun());
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

        protected void sendBullet(float bx, float by, float vx, float vy, Player p, long id)
        {
            if (Weapons.instance.scene == null)
                return;
            Weapons.instance.scene.Broadcast("spawn_bullet", s =>
            {
                var writer = new BinaryWriter(s, Encoding.UTF8, false);

                writer.Write(id);
                writer.Write(p.id);
                writer.Write(bx);
                writer.Write(by);
                writer.Write(vx);
                writer.Write(vy);
                writer.Write(size);
            }, PacketPriority.MEDIUM_PRIORITY, PacketReliability.RELIABLE);
        }

        protected void CalcNextBullet(Player p, float bx, float by, long time)
        {
            double vx = (double)(bx - p.pos_x);
            double vy = (double)(by - p.pos_y);

            Normalize(ref vx, ref vy);

            vx = vx + ((rand.NextDouble() - 1) % spread);
            vy = vy + ((rand.NextDouble() - 1) % spread);

            Normalize(ref vx, ref vy);

            bx = p.pos_x + (float)vx * 1.5f + (p.vect_x * (float)(((Weapons.instance.scene.GetComponent<IEnvironment>().Clock - time)) / 10));
            by = p.pos_y + (float)vy * 1.5f + (p.vect_y * (float)(((Weapons.instance.scene.GetComponent<IEnvironment>().Clock - time)) / 10));

            vx = vx * speed;
            vy = vy * speed;

            long id = Weapons.instance.id;
            //Weapons.instance.bullets.TryAdd(id, new Bullet(id, p, time));
            Weapons.instance.id++;
            if (Weapons.instance.id > 2000000)
                Weapons.instance.id = 0;
            sendBullet(bx, by, (float) vx, (float) vy, p, id);
        }

        protected void Normalize(ref double x, ref double y)
        {
            double length;

            length = Math.Sqrt((x * x) + (y * y));

            x = x / length;
            y = y / length;

        }

        virtual public Task Fire(Player p, float target_x, float target_y, long time)
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

        public override Task Fire(Player p, float target_x, float target_y, long time)
        {
            CalcNextBullet(p, target_x, target_y, time);
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
            speed = 10;
            spread = 0.05f;
        }

        public override async Task Fire(Player p, float target_x, float target_y, long time)
        {
            CalcNextBullet(p, target_x, target_y, time);
            await Task.Delay(100);
            CalcNextBullet(p, target_x, target_y, time);
            await Task.Delay(100);
            CalcNextBullet(p, target_x, target_y, time);
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
            spread = .5f;
        }

        public override Task Fire(Player p, float target_x, float target_y, long time)
        {
            CalcNextBullet(p, target_x, target_y, time);
            return Task.FromResult(true);
        }
    }

    public class WeaponShotgun : Weapon
    {
        public WeaponShotgun()
        {
            name = "machine gun";
            cooldown = 2000;
            damage = 15;
            size = 5;
            speed = 5;
            spread = 1f;
        }

        public override Task Fire(Player p, float target_x, float target_y, long time)
        {
            CalcNextBullet(p, target_x, target_y, time);
            CalcNextBullet(p, target_x, target_y, time);
            CalcNextBullet(p, target_x, target_y, time);
            CalcNextBullet(p, target_x, target_y, time);
            CalcNextBullet(p, target_x, target_y, time);
            return Task.FromResult(true);
        }
    }
}
