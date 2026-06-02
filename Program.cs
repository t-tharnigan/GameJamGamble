using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

class Game
{
    const int ScreenW = 1000;
    const int ScreenH = 700;

    // ==================== WAPENS ====================
    enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    static Color RarityToColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return new Color(160, 160, 160, 255);
            case Rarity.Uncommon:  return new Color(76, 204, 76, 255);
            case Rarity.Rare:      return new Color(76, 128, 255, 255);
            case Rarity.Epic:      return new Color(178, 76, 255, 255);
            case Rarity.Legendary: return new Color(255, 178, 25, 255);
        }
        return Color.White;
    }

    class Weapon
    {
        public string Name;
        public Rarity Rarity;
        public float Damage;
        public float FireRate;      // seconden tussen schoten
        public float BulletSpeed;
        public int BulletsPerShot;
        public float Spread;        // graden
        public float Pierce = 0;    // hoeveel enemies een kogel doorboort (0 = stopt bij eerste)
        public bool Homing = false; // kogels sturen naar dichtstbijzijnde enemy

        public Color Color() => RarityToColor(Rarity);

        // Ruwe DPS-score om wapens te vergelijken
        public float Score() => Damage * (1f / FireRate) * BulletsPerShot * (1f + Pierce * 0.5f) * (Homing ? 1.3f : 1f);
    }

    // Grotere, uniekere loot-pool met speciale eigenschappen
    static List<Weapon> WeaponPool = new List<Weapon>
    {
        // Common
        new Weapon { Name = "Pistol",        Rarity = Rarity.Common,    Damage = 10, FireRate = 0.35f, BulletSpeed = 500, BulletsPerShot = 1, Spread = 2 },
        new Weapon { Name = "SMG",           Rarity = Rarity.Common,    Damage = 8,  FireRate = 0.12f, BulletSpeed = 550, BulletsPerShot = 1, Spread = 6 },
        new Weapon { Name = "Pea Shooter",   Rarity = Rarity.Common,    Damage = 6,  FireRate = 0.20f, BulletSpeed = 600, BulletsPerShot = 1, Spread = 3 },
        // Uncommon
        new Weapon { Name = "Rifle",         Rarity = Rarity.Uncommon,  Damage = 18, FireRate = 0.30f, BulletSpeed = 650, BulletsPerShot = 1, Spread = 1 },
        new Weapon { Name = "Shotgun",       Rarity = Rarity.Uncommon,  Damage = 7,  FireRate = 0.70f, BulletSpeed = 480, BulletsPerShot = 6, Spread = 18 },
        new Weapon { Name = "Twin Pistols",  Rarity = Rarity.Uncommon,  Damage = 11, FireRate = 0.22f, BulletSpeed = 560, BulletsPerShot = 2, Spread = 8 },
        // Rare
        new Weapon { Name = "Burst Rifle",   Rarity = Rarity.Rare,      Damage = 22, FireRate = 0.25f, BulletSpeed = 700, BulletsPerShot = 2, Spread = 3 },
        new Weapon { Name = "Heavy MG",      Rarity = Rarity.Rare,      Damage = 14, FireRate = 0.08f, BulletSpeed = 600, BulletsPerShot = 1, Spread = 7 },
        new Weapon { Name = "Piercer",       Rarity = Rarity.Rare,      Damage = 20, FireRate = 0.40f, BulletSpeed = 800, BulletsPerShot = 1, Spread = 0, Pierce = 3 },
        // Epic
        new Weapon { Name = "Plasma Gun",    Rarity = Rarity.Epic,      Damage = 40, FireRate = 0.40f, BulletSpeed = 750, BulletsPerShot = 1, Spread = 0, Pierce = 2 },
        new Weapon { Name = "Scatter Storm", Rarity = Rarity.Epic,      Damage = 12, FireRate = 0.55f, BulletSpeed = 600, BulletsPerShot = 10, Spread = 22 },
        new Weapon { Name = "Seeker",        Rarity = Rarity.Epic,      Damage = 24, FireRate = 0.30f, BulletSpeed = 450, BulletsPerShot = 1, Spread = 0, Homing = true },
        // Legendary
        new Weapon { Name = "Railgun",       Rarity = Rarity.Legendary, Damage = 90, FireRate = 0.60f, BulletSpeed = 1100, BulletsPerShot = 1, Spread = 0, Pierce = 99 },
        new Weapon { Name = "Doom Cannon",   Rarity = Rarity.Legendary, Damage = 30, FireRate = 0.06f, BulletSpeed = 800, BulletsPerShot = 1, Spread = 4 },
        new Weapon { Name = "Swarm Lord",    Rarity = Rarity.Legendary, Damage = 22, FireRate = 0.18f, BulletSpeed = 500, BulletsPerShot = 3, Spread = 10, Homing = true, Pierce = 1 },
        new Weapon { Name = "Annihilator",   Rarity = Rarity.Legendary, Damage = 55, FireRate = 0.25f, BulletSpeed = 900, BulletsPerShot = 2, Spread = 2, Pierce = 4 },
    };

    // ==================== CHESTS ====================
    // Chest-rarity bepaalt kosten EN de loot-tabel (betere chests = betere kansen)
    enum ChestTier { Wooden, Silver, Gold, Diamond }

    class ChestDef
    {
        public ChestTier Tier;
        public int Cost;
        public string Label;
        public Color Color;
        // gewogen kansen per wapenrarity: {common, uncommon, rare, epic, legendary}
        public double[] Weights;
    }

    static Dictionary<ChestTier, ChestDef> ChestDefs = new Dictionary<ChestTier, ChestDef>
    {
        { ChestTier.Wooden,  new ChestDef { Tier = ChestTier.Wooden,  Cost = 30,  Label = "Houten Chest",  Color = new Color(150, 110, 60, 255),  Weights = new double[]{ 0.60, 0.27, 0.10, 0.025, 0.005 } } },
        { ChestTier.Silver,  new ChestDef { Tier = ChestTier.Silver,  Cost = 80,  Label = "Zilveren Chest", Color = new Color(190, 195, 205, 255), Weights = new double[]{ 0.35, 0.35, 0.20, 0.08,  0.02  } } },
        { ChestTier.Gold,    new ChestDef { Tier = ChestTier.Gold,    Cost = 180, Label = "Gouden Chest",   Color = new Color(230, 190, 50, 255),  Weights = new double[]{ 0.15, 0.30, 0.32, 0.18,  0.05  } } },
        { ChestTier.Diamond, new ChestDef { Tier = ChestTier.Diamond, Cost = 400, Label = "Diamanten Chest",Color = new Color(110, 220, 240, 255), Weights = new double[]{ 0.05, 0.15, 0.35, 0.30,  0.15  } } },
    };

    class Chest { public Vector2 Pos; public ChestTier Tier; }

    // ==================== ENEMIES ====================
    enum EnemyType { Grunt, Fast, Tank, Boss }

    class EnemyDef
    {
        public EnemyType Type;
        public float Health;
        public float Speed;
        public int Coins;
        public float Radius;
        public int Damage;     // contactschade per frame-tik
        public Color Color;
    }

    static Dictionary<EnemyType, EnemyDef> EnemyDefs = new Dictionary<EnemyType, EnemyDef>
    {
        { EnemyType.Grunt, new EnemyDef { Type = EnemyType.Grunt, Health = 30,  Speed = 95,  Coins = 5,  Radius = 16, Damage = 1, Color = new Color(220, 60, 60, 255) } },
        { EnemyType.Fast,  new EnemyDef { Type = EnemyType.Fast,  Health = 18,  Speed = 190, Coins = 8,  Radius = 12, Damage = 1, Color = new Color(240, 150, 40, 255) } },
        { EnemyType.Tank,  new EnemyDef { Type = EnemyType.Tank,  Health = 120, Speed = 55,  Coins = 18, Radius = 24, Damage = 2, Color = new Color(150, 60, 160, 255) } },
        { EnemyType.Boss,  new EnemyDef { Type = EnemyType.Boss,  Health = 600, Speed = 70,  Coins = 120,Radius = 38, Damage = 3, Color = new Color(255, 40, 90, 255) } },
    };

    class Enemy
    {
        public Vector2 Pos;
        public float Health;
        public float MaxHealth;
        public EnemyType Type;
        public EnemyDef Def;
    }

    // ==================== BULLET ====================
    class Bullet
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Damage;
        public bool Alive = true;
        public int PierceLeft;
        public bool Homing;
        public HashSet<Enemy> Hit = new HashSet<Enemy>(); // niet 2x dezelfde enemy raken
    }

    static Random rng = new Random();

    // ==================== STATE ====================
    static Vector2 playerPos = new Vector2(ScreenW / 2f, ScreenH / 2f);
    static float playerSpeed = 260f;
    static int playerHealth = 100;
    static Weapon currentWeapon;
    static int coins = 0;
    static float fireCooldown = 0f;
    static float gameTime = 0f; // verstreken tijd in seconden, drijft de difficulty

    static List<Bullet> bullets = new List<Bullet>();
    static List<Enemy> enemies = new List<Enemy>();
    static List<Chest> chests = new List<Chest>();

    static float enemySpawnTimer = 0f;
    static float chestSpawnTimer = 3f;
    static float bossTimer = 60f; // eerste boss na 60s

    // Case-animatie
    static bool caseOpen = false;
    static List<Weapon> caseStrip = new List<Weapon>();
    static float caseScroll = 0f, caseTargetScroll = 0f, caseTimer = 0f;
    static float caseDuration = 4.5f, caseResultTimer = 0f;
    static int caseWinIndex = 0;
    static bool caseFinished = false;
    static ChestTier caseTier;
    const int SlotWidth = 140;

    static void Main()
    {
        Raylib.InitWindow(ScreenW, ScreenH, "Top-Down Shooter");
        Raylib.SetTargetFPS(60);
        currentWeapon = WeaponPool[0];

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();
            if (caseOpen) UpdateCase(dt);
            else if (playerHealth > 0) UpdateGame(dt);
            Draw();
        }
        Raylib.CloseWindow();
    }

    // ==================== GAMEPLAY ====================
    static void UpdateGame(float dt)
    {
        gameTime += dt;

        // Beweging
        Vector2 move = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W)) move.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) move.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A)) move.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) move.X += 1;
        if (move.LengthSquared() > 0) move = Vector2.Normalize(move);
        playerPos += move * playerSpeed * dt;
        playerPos.X = Math.Clamp(playerPos.X, 20, ScreenW - 20);
        playerPos.Y = Math.Clamp(playerPos.Y, 20, ScreenH - 20);

        // Richten
        Vector2 mouse = Raylib.GetMousePosition();
        Vector2 aimDir = mouse - playerPos;
        if (aimDir.LengthSquared() > 0) aimDir = Vector2.Normalize(aimDir);

        // Schieten
        fireCooldown -= dt;
        if (Raylib.IsMouseButtonDown(MouseButton.Left) && fireCooldown <= 0f)
        {
            Shoot(aimDir);
            fireCooldown = currentWeapon.FireRate;
        }

        // Chest openen
        if (Raylib.IsKeyPressed(KeyboardKey.E))
        {
            for (int i = chests.Count - 1; i >= 0; i--)
            {
                ChestDef def = ChestDefs[chests[i].Tier];
                if (Vector2.Distance(playerPos, chests[i].Pos) < 55 && coins >= def.Cost)
                {
                    coins -= def.Cost;
                    caseTier = chests[i].Tier;
                    chests.RemoveAt(i);
                    StartCase();
                    break;
                }
            }
        }

        // Bullets
        foreach (var b in bullets)
        {
            if (b.Homing)
            {
                Enemy near = FindNearestEnemy(b.Pos);
                if (near != null)
                {
                    Vector2 want = Vector2.Normalize(near.Pos - b.Pos);
                    Vector2 cur = Vector2.Normalize(b.Vel);
                    Vector2 newDir = Vector2.Normalize(cur + want * 3f * dt);
                    b.Vel = newDir * b.Vel.Length();
                }
            }
            b.Pos += b.Vel * dt;
            if (b.Pos.X < -50 || b.Pos.X > ScreenW + 50 || b.Pos.Y < -50 || b.Pos.Y > ScreenH + 50)
                b.Alive = false;
        }

        // Enemies bewegen + contactschade
        foreach (var e in enemies)
        {
            Vector2 dir = playerPos - e.Pos;
            if (dir.LengthSquared() > 0) dir = Vector2.Normalize(dir);
            e.Pos += dir * e.Def.Speed * dt;
            if (Vector2.Distance(e.Pos, playerPos) < e.Def.Radius + 16)
                playerHealth -= e.Def.Damage;
        }

        // Bullet vs enemy (met pierce)
        foreach (var b in bullets)
        {
            if (!b.Alive) continue;
            foreach (var e in enemies)
            {
                if (e.Health <= 0 || b.Hit.Contains(e)) continue;
                if (Vector2.Distance(b.Pos, e.Pos) < e.Def.Radius + 4)
                {
                    e.Health -= b.Damage;
                    b.Hit.Add(e);
                    if (e.Health <= 0) coins += e.Def.Coins;
                    if (b.PierceLeft <= 0) { b.Alive = false; break; }
                    b.PierceLeft--;
                }
            }
        }

        bullets.RemoveAll(b => !b.Alive);
        enemies.RemoveAll(e => e.Health <= 0);

        // ----- Difficulty over tijd -----
        // Spawn-interval daalt van 1.6s naar 0.35s over ~3 minuten
        float spawnInterval = Math.Max(0.35f, 1.6f - gameTime * 0.007f);
        enemySpawnTimer -= dt;
        if (enemySpawnTimer <= 0f)
        {
            SpawnEnemy();
            enemySpawnTimer = spawnInterval;
        }

        // Boss elke 60s
        bossTimer -= dt;
        if (bossTimer <= 0f)
        {
            SpawnBoss();
            bossTimer = 60f;
        }

        // Chests spawnen; betere tiers worden waarschijnlijker naarmate de tijd vordert
        chestSpawnTimer -= dt;
        if (chestSpawnTimer <= 0f && chests.Count < 3)
        {
            chests.Add(new Chest
            {
                Pos = new Vector2(rng.Next(60, ScreenW - 60), rng.Next(60, ScreenH - 60)),
                Tier = RollChestTier()
            });
            chestSpawnTimer = 11f;
        }
    }

    static Enemy FindNearestEnemy(Vector2 from)
    {
        Enemy best = null;
        float bestD = float.MaxValue;
        foreach (var e in enemies)
        {
            if (e.Health <= 0) continue;
            float d = Vector2.DistanceSquared(from, e.Pos);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    static void Shoot(Vector2 dir)
    {
        float baseAngle = MathF.Atan2(dir.Y, dir.X);
        for (int i = 0; i < currentWeapon.BulletsPerShot; i++)
        {
            float spreadRad = (float)((rng.NextDouble() * 2 - 1) * currentWeapon.Spread * Math.PI / 180.0);
            float a = baseAngle + spreadRad;
            Vector2 vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * currentWeapon.BulletSpeed;
            bullets.Add(new Bullet
            {
                Pos = playerPos, Vel = vel, Damage = currentWeapon.Damage,
                PierceLeft = (int)currentWeapon.Pierce, Homing = currentWeapon.Homing
            });
        }
    }

    static Vector2 RandomEdge()
    {
        switch (rng.Next(4))
        {
            case 0: return new Vector2(rng.Next(ScreenW), -30);
            case 1: return new Vector2(rng.Next(ScreenW), ScreenH + 30);
            case 2: return new Vector2(-30, rng.Next(ScreenH));
            default: return new Vector2(ScreenW + 30, rng.Next(ScreenH));
        }
    }

    static void SpawnEnemy()
    {
        // Naarmate de tijd vordert komen er gevaarlijkere types bij
        EnemyType type = EnemyType.Grunt;
        double r = rng.NextDouble();
        if (gameTime > 90 && r < 0.20) type = EnemyType.Tank;
        else if (gameTime > 45 && r < 0.45) type = EnemyType.Fast;
        else if (gameTime > 20 && r < 0.30) type = EnemyType.Fast;

        EnemyDef def = EnemyDefs[type];
        // health schaalt licht mee met de tijd
        float scale = 1f + gameTime / 180f;
        enemies.Add(new Enemy
        {
            Pos = RandomEdge(), Type = type, Def = def,
            Health = def.Health * scale, MaxHealth = def.Health * scale
        });
    }

    static void SpawnBoss()
    {
        EnemyDef def = EnemyDefs[EnemyType.Boss];
        float scale = 1f + gameTime / 120f;
        enemies.Add(new Enemy
        {
            Pos = RandomEdge(), Type = EnemyType.Boss, Def = def,
            Health = def.Health * scale, MaxHealth = def.Health * scale
        });
    }

    static ChestTier RollChestTier()
    {
        // Hoe later in de game, hoe meer kans op betere chests
        double r = rng.NextDouble();
        double bonus = Math.Min(0.5, gameTime / 240.0); // schuift kansen omhoog
        if (r < 0.50 - bonus) return ChestTier.Wooden;
        if (r < 0.80 - bonus * 0.5) return ChestTier.Silver;
        if (r < 0.95) return ChestTier.Gold;
        return ChestTier.Diamond;
    }

    // ==================== CASE OPENING ====================
    static void StartCase()
    {
        caseOpen = true; caseFinished = false;
        caseTimer = 0f; caseResultTimer = 0f; caseScroll = 0f;

        Weapon won = RollWeaponFromChest(caseTier);
        caseStrip.Clear();
        int total = 50;
        caseWinIndex = total - 6;
        for (int i = 0; i < total; i++)
            caseStrip.Add(i == caseWinIndex ? won : RollWeaponFromChest(caseTier));

        float markerX = ScreenW / 2f;
        float slotCenter = caseWinIndex * SlotWidth + SlotWidth / 2f;
        float offset = (float)((rng.NextDouble() * 2 - 1) * SlotWidth * 0.3f);
        caseTargetScroll = markerX - slotCenter + offset;
    }

    static void UpdateCase(float dt)
    {
        if (!caseFinished)
        {
            caseTimer += dt;
            float t = Math.Clamp(caseTimer / caseDuration, 0f, 1f);
            float eased = 1f - MathF.Pow(1f - t, 3f);
            caseScroll = eased * caseTargetScroll;
            if (t >= 1f) { caseScroll = caseTargetScroll; caseFinished = true; }
        }
        else
        {
            caseResultTimer += dt;
            if (caseResultTimer > 0.01f && caseResultTimer < 0.05f)
            {
                Weapon won = caseStrip[caseWinIndex];
                if (won.Score() > currentWeapon.Score())
                    currentWeapon = won;
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Space) || caseResultTimer > 3f)
                caseOpen = false;
        }
    }

    static Weapon RollWeaponFromChest(ChestTier tier)
    {
        double[] w = ChestDefs[tier].Weights;
        double roll = rng.NextDouble();
        double acc = 0;
        Rarity target = Rarity.Common;
        Rarity[] order = { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary };
        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (roll < acc) { target = order[i]; break; }
        }
        var matches = WeaponPool.FindAll(x => x.Rarity == target);
        if (matches.Count == 0) return WeaponPool[rng.Next(WeaponPool.Count)];
        return matches[rng.Next(matches.Count)];
    }

    // ==================== TEKENEN ====================
    static void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(28, 28, 36, 255));

        // Chests met prijslabel erboven
        foreach (var c in chests)
        {
            ChestDef def = ChestDefs[c.Tier];
            Raylib.DrawRectangle((int)c.Pos.X - 20, (int)c.Pos.Y - 15, 40, 30, def.Color);
            Raylib.DrawRectangleLines((int)c.Pos.X - 20, (int)c.Pos.Y - 15, 40, 30, Color.White);

            // prijslabel — altijd zichtbaar
            string price = $"{def.Cost}c";
            int pw = Raylib.MeasureText(price, 16);
            bool canAfford = coins >= def.Cost;
            Color priceCol = canAfford ? Color.Yellow : new Color(150, 90, 90, 255);
            Raylib.DrawText(price, (int)c.Pos.X - pw / 2, (int)c.Pos.Y - 38, 16, priceCol);

            // tier-naam + open-hint als je dichtbij bent
            if (Vector2.Distance(playerPos, c.Pos) < 55)
            {
                string hint = canAfford ? $"[E] {def.Label}" : $"{def.Label} (te weinig)";
                int hw = Raylib.MeasureText(hint, 14);
                Raylib.DrawText(hint, (int)c.Pos.X - hw / 2, (int)c.Pos.Y + 20, 14, def.Color);
            }
        }

        // Enemies
        foreach (var e in enemies)
        {
            Raylib.DrawCircleV(e.Pos, e.Def.Radius, e.Def.Color);
            if (e.Type == EnemyType.Boss)
                Raylib.DrawCircleLines((int)e.Pos.X, (int)e.Pos.Y, e.Def.Radius + 3, Color.White);
            float barW = e.Def.Radius * 2;
            float hp = barW * (e.Health / e.MaxHealth);
            Raylib.DrawRectangle((int)(e.Pos.X - e.Def.Radius), (int)(e.Pos.Y - e.Def.Radius - 8), (int)barW, 4, Color.DarkGray);
            Raylib.DrawRectangle((int)(e.Pos.X - e.Def.Radius), (int)(e.Pos.Y - e.Def.Radius - 8), (int)hp, 4, Color.Green);
        }

        // Bullets
        foreach (var b in bullets)
            Raylib.DrawCircleV(b.Pos, 4, currentWeapon.Color());

        // Speler
        Raylib.DrawCircleV(playerPos, 16, new Color(80, 160, 255, 255));
        Vector2 mouse = Raylib.GetMousePosition();
        Vector2 aim = mouse - playerPos;
        if (aim.LengthSquared() > 0) aim = Vector2.Normalize(aim);
        Raylib.DrawLineEx(playerPos, playerPos + aim * 26, 4, Color.White);

        // HUD
        Raylib.DrawText($"Coins: {coins}", 14, 14, 24, Color.Yellow);
        Raylib.DrawText($"HP: {Math.Max(0, playerHealth)}", 14, 44, 24, Color.White);
        Raylib.DrawText($"Wapen: {currentWeapon.Name}", 14, 74, 22, currentWeapon.Color());
        int mins = (int)gameTime / 60, secs = (int)gameTime % 60;
        string timeTxt = $"Tijd: {mins:0}:{secs:00}";
        Raylib.DrawText(timeTxt, ScreenW - Raylib.MeasureText(timeTxt, 22) - 14, 14, 22, Color.White);
        string bossTxt = $"Boss in: {Math.Max(0, (int)bossTimer)}s";
        Raylib.DrawText(bossTxt, ScreenW - Raylib.MeasureText(bossTxt, 18) - 14, 42, 18, new Color(255, 120, 140, 255));

        // Chest-prijslegende rechtsonder
        DrawChestLegend();

        if (playerHealth <= 0)
            Raylib.DrawText("GAME OVER", ScreenW / 2 - 130, ScreenH / 2 - 30, 60, Color.Red);

        if (caseOpen) DrawCase();

        Raylib.EndDrawing();
    }

    static void DrawChestLegend()
    {
        int x = ScreenW - 200, y = ScreenH - 120;
        Raylib.DrawRectangle(x - 10, y - 10, 200, 115, new Color(0, 0, 0, 120));
        Raylib.DrawText("Chest prijzen", x, y, 16, Color.White);
        int i = 1;
        foreach (var kv in ChestDefs)
        {
            ChestDef d = kv.Value;
            int ly = y + i * 22;
            Raylib.DrawRectangle(x, ly + 2, 14, 14, d.Color);
            Raylib.DrawText($"{d.Label}: {d.Cost}c", x + 20, ly, 14, d.Color);
            i++;
        }
    }

    static void DrawCase()
    {
        Raylib.DrawRectangle(0, 0, ScreenW, ScreenH, new Color(0, 0, 0, 200));

        ChestDef def = ChestDefs[caseTier];
        string title = def.Label;
        int tw0 = Raylib.MeasureText(title, 28);
        Raylib.DrawText(title, ScreenW / 2 - tw0 / 2, ScreenH / 2 - 140, 28, def.Color);

        int stripY = ScreenH / 2 - 60, stripH = 120;
        Raylib.DrawRectangle(0, stripY, ScreenW, stripH, new Color(20, 20, 28, 255));

        for (int i = 0; i < caseStrip.Count; i++)
        {
            float x = caseScroll + i * SlotWidth;
            if (x + SlotWidth < 0 || x > ScreenW) continue;
            Weapon w = caseStrip[i];
            Color col = w.Color();
            int pad = 8;
            Raylib.DrawRectangle((int)x + pad, stripY + pad, SlotWidth - pad * 2, stripH - pad * 2, new Color(col.R, col.G, col.B, (byte)60));
            Raylib.DrawRectangleLines((int)x + pad, stripY + pad, SlotWidth - pad * 2, stripH - pad * 2, col);
            int fs = 16, tw = Raylib.MeasureText(w.Name, fs);
            Raylib.DrawText(w.Name, (int)(x + SlotWidth / 2 - tw / 2), stripY + stripH / 2 - 8, fs, col);
        }

        Raylib.DrawRectangle(ScreenW / 2 - 2, stripY - 10, 4, stripH + 20, Color.Red);
        Raylib.DrawTriangle(
            new Vector2(ScreenW / 2 - 10, stripY - 10),
            new Vector2(ScreenW / 2 + 10, stripY - 10),
            new Vector2(ScreenW / 2, stripY + 4), Color.Red);

        if (caseFinished)
        {
            Weapon won = caseStrip[caseWinIndex];
            string txt = $"Je kreeg: {won.Name} ({won.Rarity})";
            int fs = 30, tw = Raylib.MeasureText(txt, fs);
            Raylib.DrawText(txt, ScreenW / 2 - tw / 2, stripY + stripH + 30, fs, won.Color());
            Raylib.DrawText("[Spatie] om door te gaan", ScreenW / 2 - 120, stripY + stripH + 70, 18, Color.LightGray);
        }
    }
}