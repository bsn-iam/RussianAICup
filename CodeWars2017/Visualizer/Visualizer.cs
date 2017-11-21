﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Visualizer
{
    public class Visualizer
    {
        #region System

        public static void CreateForm()
        {
            if (_form == null)
            {
                _thread = new Thread(_showWindow);
                _thread.Start();
                Thread.Sleep(2000);
            }
        }

        private static void _showWindow()
        {
            try
            {
                _form = new MainForm();
                _form.ShowDialog();
                _form.Focus();
            }
            catch (Exception e)
            {
                MyStrategy.Universe.Print(e.Message + e.StackTrace);
            }

        }

        public static MainForm _form;
        private static Thread _thread;
        private static Graphics _graphics;

        private delegate void DrawDelegate();

        private static void DrawCircle(Color color, double x, double y, double radius, float width = 0)
        {
            var pen = width > 0 ? new Pen(color, width) : new Pen(color);
            _graphics.DrawEllipse(pen, _X(x - radius), _Y(y - radius), _S(radius * 2), _S(radius * 2));
        }

        private static void FillCircle(Color color, double x, double y, double radius)
        {
            _graphics.FillEllipse(new SolidBrush(color), _X(x - radius), _Y(y - radius), _S(radius * 2), _S(radius * 2));
        }

        private static void FillRect(Color color, double x, double y, double w, double h)
        {
            _graphics.FillRectangle(new SolidBrush(color), _X(x), _Y(y), _S(w), _S(h));
        }

        private static void DrawLine(Color color, double x, double y, double X, double Y, float width = 0F)
        {
            _graphics.DrawLine(new Pen(color, width), _X(x), _Y(y), _X(X), _Y(Y));
        }

        private static void DrawPie(Color color, double x, double y, double r, double startAngle, double endAngle, float width = 0F)
        {
            startAngle = Geom.ToDegrees(startAngle);
            endAngle = Geom.ToDegrees(endAngle);
            _graphics.DrawPie(new Pen(color), new Rectangle(_X(x - r), _Y(y - r), _S(2*r), _S(2*r)), (float)startAngle, (float)(endAngle - startAngle));
        }

        private static void DrawText(string text, double size, Brush brush, double x, double y)
        {
            var font = new Font("Comic Sans MS", _S(size));
            _graphics.DrawString(text, font, brush, _X(x), _Y(y));
        }

        private static double _lookX = 0, _lookY = 0, _scale = 1.5;

        private static int _X(double x)
        {
            return (int)((x - _lookX) / _scale);
        }

        private static int _Y(double y)
        {
            return (int)((y - _lookY) / _scale);
        }

        private static int _S(double x)
        {
            return (int)Math.Ceiling(x / _scale);
        }

        public static List<object[]> SegmentsDrawQueue = new List<object[]>();
        public static List<Tuple<Point, double>> DangerPoints;
        public static Dictionary<long, Point[]>
            Projectiles = new Dictionary<long, Point[]>();

        public class Color01
        {
            public double R, G, B;

            public Color01(double r, double g, double b)
            {
                R = r;
                G = g;
                B = b;
            }

            public Color ToColor()
            {
                return Color.FromArgb((int)(255 * R), (int)(255 * G), (int)(255 * B));
            }
        }

        private static Color01 _grad2(Color01 col1, Color01 col2, double x)
        {
            return new Color01(
                (col2.R - col1.R) * x + col1.R,
                (col2.G - col1.G) * x + col1.G,
                (col2.B - col1.B) * x + col1.B
            );
        }

        public static Color01[] BadColors = new[] {
            new Color01(0x8B / 255.0, 0, 0),// red!!
            new Color01(1, 0, 0),// red
            new Color01(1, 69 / 255.0, 0),// orange
            new Color01(1, 1, 0),// yellow
            new Color01(1, 1, 1),// white
        };

        public static Color01[] GoodColors = new[] {
            new Color01(1, 1, 1),// white
            new Color01(0, 1, 0),// green
        };

//        public static Color01 Gradient(Color01[] colors, double x)
//        {
//            var delta = 1.0 / (colors.Length - 1);
//            for (var i = 0; i < colors.Length - 1; i++)
//            {
//                var left = delta*i;
//                var right = delta * (i + 1);
//                if (left <= x && x <= right)
//                {
//                    return _grad2(colors[i], colors[i + 1], (x - left) * (colors.Length - 1));
//                }
//            }
//            //throw new Exception("wrong x ranges");
//        }

        public static int DrawSince { get; set; } = 0;

        public static bool Done;

        public static void Draw()
        {
            if (_form.InvokeRequired)
            {
                _form.BeginInvoke(new DrawDelegate(Draw), new object[] {});
                return;
            }

            
            Done = false;
            if (MyStrategy.Universe.World.TickIndex >= DrawSince)
                _draw();
            SegmentsDrawQueue.Clear();
            Done = true;
        }

        #endregion

        public static void _draw()
        {
            var panel = _form.panel;

            _form.tickLabel.Text = MyStrategy.Universe.World.TickIndex + "";
            
            var drawArea = new Bitmap(panel.Size.Width, panel.Size.Height);
            panel.Image = drawArea;
            _graphics = Graphics.FromImage(drawArea);

            var tar = MyStrategy.Universe.MyUnits.GetEnumeration().FirstOrDefault(x => x.Id.ToString() == _form.lookAtTextBox.Text.Trim());
            if (tar != null)
                LookAt(new Point(tar));

            #region WarFog

            // туман войны
            FillRect(Color.AntiqueWhite, 0, 0, MyStrategy.Universe.World.Width, MyStrategy.Universe.World.Height);
            foreach (var unit in MyStrategy.Universe.MyUnits.GetEnumeration())
            {
                FillCircle(Color.White, unit.X, unit.Y, unit.VisionRange);
            }
            #endregion

            #region BonusMap

            var bonusMap = MyStrategy.BonusCalculator.BonusMapList.FirstOrDefault();
            if (bonusMap.Value != null)
            {
                var tileList = bonusMap.Value.GetTileList();

                foreach (var tile in tileList)
                    if (tile.Value != 0)
                    { 
                        var color01 = new Color01(1, 1-tile.Value, 1-tile.Value);
                        FillRect(color01.ToColor(), tile.CenterPosition.X, tile.CenterPosition.Y, tile.Size * 1, tile.Size * 1);
                        //DrawText($"{tile.Value:f2}", 1, Brushes.Black, tile.CenterPosition.X, tile.CenterPosition.Y);
                    }
            }

            // var colors = new Color01[] { new Color01(0, 0, 0), new Color01(100, 100, 100), new Color01(254, 254, 254) };

//            foreach (var tile in tileList)
//            {
//                //var colors01 = Gradient(colors, tile.Value);
//            
//                //FillRect(colors01.ToColor(), tile.CenterPosition.X, tile.CenterPosition.Y, Tile.Size, Tile.Size);
//            }

            #endregion

            #region UnitsDraw

            foreach (var unit in MyStrategy.Universe.MyUnits.GetEnumeration())
            {
                switch (unit.Type)
                {
                    case VehicleType.Fighter:
                        DrawUnit(Color.Blue, unit, "F"); break;
                    case VehicleType.Helicopter:
                        DrawUnit(Color.Brown, unit, "H"); break;
                    case VehicleType.Tank:
                        DrawUnit(Color.Red, unit, "T"); break;
                    case VehicleType.Arrv:
                        DrawUnit(Color.Green, unit, "A"); break;
                    case VehicleType.Ifv:
                        DrawUnit(Color.Gray, unit, "I"); break;
                }
            }


            foreach (var unit in MyStrategy.Universe.OppUnits.GetEnumeration())
            {
                switch (unit.Type)
                {
                    case VehicleType.Fighter:
                        DrawUnit(Color.DodgerBlue, unit, "F");
                        break;
                    case VehicleType.Helicopter:
                        DrawUnit(Color.SandyBrown, unit, "H");
                        break;
                    case VehicleType.Tank:
                        DrawUnit(Color.LightPink, unit, "T");
                        break;
                    case VehicleType.Arrv:
                        DrawUnit(Color.LightGreen, unit, "A");
                        break;
                    case VehicleType.Ifv:
                        DrawUnit(Color.LightGray, unit, "I");
                        break;
                }
            }

            #endregion

            #region Predictions

            var allRealUnits = MyStrategy.Universe.OppUnits.GetCombinedList(MyStrategy.Universe.MyUnits);

            foreach (var currectUnit in allRealUnits)
            {
                var expectedTickForNextUpdate = MyStrategy.Universe.World.TickIndex;
                if (currectUnit.PlayerId == MyStrategy.Universe.Player.Id)
                {
                    var correspondingSquad = MyStrategy.SquadCalculator.SquadList.GetSquadByUnit(currectUnit);
                    if (correspondingSquad == null)
                        continue;
                    expectedTickForNextUpdate = MyStrategy.Universe.World.TickIndex +
                                                correspondingSquad.ExpectedTicksToNextUpdate;
                }
                else
                {
                    // nearestMyUnit = MyStrategy.Universe.MyUnits.
                    // var correspondingSquad = MyStrategy.SquadCalculator.SquadList.GetSquadByUnit(nearestMyUnit);
                    // expectedTickForNextUpdate = MyStrategy.Universe.World.TickIndex + correspondingSquad.ExpectedTicksToNextUpdate;
                    expectedTickForNextUpdate = MyStrategy.Universe.World.TickIndex + 60;
                }
                    

                var predictedState = MyStrategy.Predictor.GetStateOnTick(expectedTickForNextUpdate);
                var allPredictedUnits = predictedState.OppUnits.GetCombinedList(predictedState.MyUnits);

                foreach (var predictedUnit in allPredictedUnits)
                {
                    if (predictedUnit.Id == currectUnit.Id)
                    {
                        var vectorLength = currectUnit.GetDistanceTo(predictedUnit);
                        if (vectorLength > 2)
                            DrawLine(Color.Gray, currectUnit.X, currectUnit.Y, predictedUnit.X, predictedUnit.Y, 1);
                    }
                }
            }


            #endregion

            #region MoveOrders

            foreach (var order in MyStrategy.MoveOrder)
            {
                var unit = MyStrategy.Universe.MyUnits.FirstOrDefault(u => u.Id.Equals(order.Key));
                if (unit!=null)
                    DrawLine(Color.LightGreen, unit.X, unit.Y, order.Value.X, order.Value.Y, 2);
            }
            

            #endregion






            #region Something

            //            foreach (var seg in RoadsHelper.Roads)
            //                DrawLine(Color.Khaki, seg.A.X, seg.A.Y, seg.B.X, seg.B.Y);

            //            if (_form.gradCheckBox.Checked)
            //            {
            //                var maxDanger = DangerPoints.Max(x => x.Item2);
            //                var minDanger = DangerPoints.Min(x => x.Item2);
            //
            //                if (maxDanger > Const.Eps)
            //                {
            //                    foreach (var t in DangerPoints)
            //                    {
            //                        var pt = t.Item1;
            //                        var danger = t.Item2;
            //                        var color =
            //                            (danger >= 0 ? _grad(BadColors, 1 - danger/maxDanger) : _grad(GoodColors, danger/minDanger))
            //                                .ToColor();
            //                        FillCircle(color, pt.X, pt.Y, 4);
            //                    }
            //                }
            //            }

            //            if (_form.cellsCheckBox.Checked)
            //            {
            //                for (var i = 0; i <= MyStrategy.Universe.GridSize; i++)
            //                    for (var j = 0; j <= MyStrategy.Universe.GridSize; j++)
            //                        FillCircle(Color.Red, MyStrategy.Universe._points[i, j].X, MyStrategy.Universe._points[i, j].Y, 3);
            //            }
            #endregion

            #region Statuses

            // statuses
            //            foreach (var unit in MyStrategy.Universe.MyUnits)
            //            {
            //                if (unit.RemainingFrozen > 0)
            //                    FillCircle(Color.SkyBlue, unit.X, unit.Y, unit.Radius - 3);
            //                if (unit.IsBurning)
            //                    FillCircle(Color.Orange, unit.X, unit.Y, unit.Radius - 3);
            //            }

            #endregion

            #region Wizards
            // wizards
            //            foreach (var wizard in MyStrategy.Universe.Wizards)
            //            {
            //                var w = MyStrategy.Universe.World.Wizards.FirstOrDefault(x => x.Id == wizard.Id);
            //                var color = w.IsMe ? Color.Red : (wizard.Faction == MyStrategy.Universe.Self.Faction ? Color.Blue : Color.Gold);
            //
            //                DrawCircle(color, w.X, w.Y, wizard.Radius);
            //
            //                var d = 7;
            //                for (var i = 0; i < d; i++)
            //                    if (wizard.RemainingStaffCooldownTicks >= MyStrategy.Universe.Game.StaffCooldownTicks - d)
            //                        DrawCircle(color, w.X, w.Y, wizard.Radius - (d - i));
            //
            //                DrawText(w.Life + "", 15, Brushes.Red, wizard.X - 20, wizard.Y - 35);
            //                DrawText(w.Mana + "", 15, Brushes.Blue, wizard.X - 20, wizard.Y - 15);
            //                DrawText(w.Xp + "", 15, Brushes.Green, wizard.X - 20, wizard.Y + 5);
            //
            //                DrawPie(color, wizard.X, wizard.Y, MyStrategy.Universe.Game.StaffRange, -MyStrategy.Universe.Game.StaffSector / 2.0 + wizard.Angle, MyStrategy.Universe.Game.StaffSector / 2.0 + wizard.Angle);
            //                DrawPie(color, wizard.X, wizard.Y, wizard.CastRange, -MyStrategy.Universe.Game.StaffSector / 2.0 + wizard.Angle, MyStrategy.Universe.Game.StaffSector / 2.0 + wizard.Angle);
            //
            //
            //                var statusesStr = "";
            //                if (wizard.RemainingHastened > 0)
            //                    statusesStr += "H";
            //                if (wizard.RemainingEmpowered > 0)
            //                    statusesStr += "E";
            //                if (wizard.RemainingShielded > 0)
            //                    statusesStr += "S";
            //
            //                var skillsStr = "";
            //                for (var i = 0; i < 5; i++)
            //                    skillsStr += wizard.SkillsLearnedArr[i];
            //
            //                var skillsStr2 = "";
            //                for (var i = 0; i < 5; i++)
            //                    skillsStr2 += wizard.SkillsFactorsArr[i] + wizard.AurasFactorsArr[i];
            //
            //                DrawText(statusesStr, 20, Brushes.Coral, wizard.X + 30, wizard.Y - 40);
            //                DrawText(skillsStr, 15, Brushes.Black, wizard.X + 30, wizard.Y - 15);
            //                DrawText(skillsStr2, 15, Brushes.DeepSkyBlue, wizard.X + 30, wizard.Y + 5);
            //            }


            #endregion

            #region Minions
            // minions
            //            foreach (var minion in MyStrategy.Universe.Minions)
            //            {
            //                var color = minion.IsTeammate ? Color.Blue : (minion.Faction == Faction.Neutral ? Color.Fuchsia : Color.DarkOrange);
            //
            //                DrawCircle(color, minion.X, minion.Y, minion.Radius);
            //
            //                var to = Point.ByAngle(minion.Angle) * minion.Radius + minion;
            //                DrawLine(color, minion.X, minion.Y, to.X, to.Y, 2);
            //
            //                if (minion is AOrc)
            //                {
            //                    DrawCircle(Color.Black, minion.X, minion.Y, MyStrategy.Universe.Game.OrcWoodcutterAttackRange);
            //                }
            //
            //                DrawText(minion.Life + "", 15, Brushes.Red, minion.X - 10, minion.Y - 30);
            //            }



            #endregion

            #region Trees
            // trees
            //            foreach (var tree in TreesObserver.Trees)
            //            {
            //                FillCircle(Color.Chartreuse, tree.X, tree.Y, tree.Radius);
            //            }

            #endregion

            #region Buildings

            // buildings
            //            foreach (var building in BuildingsObserver.Buildings)
            //            {
            //                FillCircle(building.IsTeammate ? Color.Blue : Color.DarkOrange, building.X, building.Y, building.Radius);
            //                DrawText(building.Life + "", 15, Brushes.Red, building.X - 10, building.Y - 30);
            //                if (building.IsBesieded)
            //                    DrawText("rush", 13, Brushes.Black, building.X - 10, building.Y);
            //                if (building.IsOpponent)
            //                    DrawCircle(Color.Red, building.X, building.Y, building.CastRange);
            //            }

            #endregion

            #region Bonuses

            // bonuses
            //            foreach (var bonus in BonusesObserver.Bonuses)
            //            {
            //                var color = bonus.Type == BonusType.Empower
            //                    ? Color.Blue
            //                    : bonus.Type == BonusType.Haste
            //                        ? Color.Aquamarine
            //                        : Color.MidnightBlue;
            //                if (bonus.Exists)
            //                    FillCircle(color, bonus.X, bonus.Y, bonus.Radius);
            //                else
            //                    DrawCircle(color, bonus.X, bonus.Y, bonus.Radius);
            //            }
            //            if (MyStrategy.Universe.NextBonusWaypoint != null)
            //                FillCircle(Color.Red, MyStrategy.Universe.NextBonusWaypoint.X, MyStrategy.Universe.NextBonusWaypoint.Y, 10);

            #endregion

            #region MapRanges

            // map ranges
            DrawLine(Color.Black, 1, 1, 1, MyStrategy.Universe.World.Height - 1);
            DrawLine(Color.Black, 1, MyStrategy.Universe.World.Height - 1, MyStrategy.Universe.World.Height - 1, MyStrategy.Universe.World.Height - 1);
            DrawLine(Color.Black, MyStrategy.Universe.World.Height - 1, 1, MyStrategy.Universe.World.Height - 1, MyStrategy.Universe.World.Height - 1);
            DrawLine(Color.Black, MyStrategy.Universe.World.Height - 1, 1, 1, 1);

            #endregion

            #region MinionSpaws

            // minions spawns
//            foreach (var pt in MagicConst.MinionAppearencePoints)
//            {
//                FillCircle(Color.Khaki, pt.X, pt.Y, 20);
//                FillCircle(Color.Khaki, MyStrategy.Universe.World.Height - pt.X, MyStrategy.Universe.World.Height - pt.Y, 20);
//            }

            #endregion

            #region ProjectTiles

            // projectiles
//            foreach (var projectile in MyStrategy.Universe.World.Projectiles)
//            {
//                var color = projectile.Type == ProjectileType.MagicMissile
//                    ? Color.Blue
//                    : projectile.Type == ProjectileType.Dart
//                        ? Color.Black
//                        : projectile.Type == ProjectileType.FrostBolt
//                            ? Color.SkyBlue
//                            : Color.Gold;
//
//                FillCircle(color, projectile.X, projectile.Y, projectile.Radius);
//                if (Projectiles.ContainsKey(projectile.Id))
//                {
//                    var pts = Projectiles[projectile.Id];
//                    DrawLine(Color.BlueViolet, pts[0].X, pts[0].Y, pts[1].X, pts[1].Y, 3);
//                }
//            }

            #endregion


            foreach (var seg in SegmentsDrawQueue)
            {
                var points = seg[0] as List<Point>;
                var pen = seg[1] as Pen;
                float width = seg.Length > 2 ? Convert.ToSingle(seg[2]) : 0F;
                for (var i = 1; i < points.Count; i++)
                    DrawLine(pen.Color, points[i].X, points[i].Y, points[i - 1].X, points[i - 1].Y, width);
            }
        }

        public static void DrawEnemyUnit(Color color, Vehicle unit, string label)
        {
            FillCircle(color, unit.X, unit.Y, 1);
            FillCircle(Color.Black, unit.X, unit.Y, 2);
            // DrawCircle(Color.CornflowerBlue, unit.X, unit.Y, unit.AerialAttackRange);
            // DrawCircle(Color.RosyBrown, unit.X, unit.Y, unit.GroundAttackRange);
            DrawText(label, 2, Brushes.Black, unit.X, unit.Y);
        }
        public static void DrawUnit(Color color, Vehicle unit, string label)
        {
            FillCircle(color, unit.X, unit.Y, 2);
            // DrawCircle(Color.CornflowerBlue, unit.X, unit.Y, unit.AerialAttackRange);
            // DrawCircle(Color.RosyBrown, unit.X, unit.Y, unit.GroundAttackRange);
            DrawText(label, 2, Brushes.Black, unit.X, unit.Y);
        }

        public static double Zoom
        {
            get
            {
                return _scale;
            }
            set
            {
                if (value > 0)
                    _scale = value;
            }
        }

        public static bool Pause = false;

        public static void LookAt(Point p, double scale = -1)
        {
            Zoom = scale;

            _lookY = p.Y - _scale*_form.panel.Height/2;
            if (_lookY < 0)
                _lookY = 0;
            if (_lookY > MyStrategy.Universe.World.Height - _scale * _form.panel.Height)
                _lookY = MyStrategy.Universe.World.Height - _scale * _form.panel.Height;

            _lookX = p.X - _scale*_form.panel.Width/2;
            if (_lookX < 0)
                _lookX = 0;
            if (_lookX > MyStrategy.Universe.World.Width - _scale * _form.panel.Width)
                _lookX = MyStrategy.Universe.World.Width - _scale * _form.panel.Width;

        }
    }
}
