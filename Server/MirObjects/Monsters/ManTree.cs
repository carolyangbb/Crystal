﻿using System;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using System.Collections.Generic;

namespace Server.MirObjects.Monsters
{
    class ManTree : ZumaMonster
    {
        protected internal ManTree(MonsterInfo info)
            : base(info)
        {
        }

        protected override void Attack()
        {
            ShockTime = 0;

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);            

            int damage = GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC]);
            if (damage == 0) return;

            AttackTime = Envir.Time + AttackSpeed;
            ActionTime = Envir.Time + 300;

            if (Envir.Random.Next(8) > 0)
            {
                if (Envir.Random.Next(4) > 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 600, Target, damage, DefenceType.ACAgility, false, false);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 600, Target, damage, DefenceType.ACAgility, true, false);
                    ActionList.Add(action);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 600, Target, damage, DefenceType.ACAgility, false, true);
                ActionList.Add(action);
            }

            if (Target.Dead)
                FindTarget();
        }

        protected override void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            bool halfMoonAttack = (bool)data[3];
            bool bouldSmashAttack = (bool)data[4];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (halfMoonAttack)
            {
                MirDirection dir = Functions.PreviousDir(Direction);

                for (int i = 0; i < 4; i++)
                {
                    Point halfMoontarget = Functions.PointMove(CurrentLocation, dir, 1);
                    dir = Functions.NextDir(dir);

                    if (!CurrentMap.ValidPoint(halfMoontarget)) continue;

                    Cell cell = CurrentMap.GetCell(halfMoontarget);
                    if (cell.Objects == null) continue;

                    for (int o = 0; o < cell.Objects.Count; o++)
                    {
                        MapObject ob = cell.Objects[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;

                        ob.Attacked(this, damage, defence);
                        break;
                    }
                }
            }

            if (bouldSmashAttack)
            {
                List<MapObject> targets = FindAllTargets(1, target.CurrentLocation);
                if (targets.Count == 0) return;

                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].IsAttackTarget(this))
                    {
                        targets[i].Attacked(this, damage, defence);
                        PoisonTarget(targets[Envir.Random.Next(targets.Count)], 5, 5, PoisonType.Stun);
                    }
                }
            }
            else
            {
                target.Attacked(this, damage, defence);
            }
        }
    }
}

