﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DamageMeter.Skills.Skill;
using DamageMeter.Skills.Skill.SkillDetail;
using DamageMeter.UI.SkillDetail;
using Data;

namespace DamageMeter.UI.Skill
{
    /// <summary>
    ///     Logique d'interaction pour Skill.xaml
    /// </summary>
    public partial class SkillDps : ISkill
    {
        public SkillDps(DamageMeter.Skills.Skill.Skill skill, SkillStats stats, Entity currentBoss, bool timedEncounter)
        {
            InitializeComponent();

            LabelName.Content = skill.SkillName;
            SkillIcon.Source = BasicTeraData.Instance.Icons.GetImage(skill.IconName);
            Update(skill, stats, currentBoss, timedEncounter);
        }

        public void Update(DamageMeter.Skills.Skill.Skill skill, SkillStats stats, Entity currentBoss, bool timedEncounter)
        {
            var skillsId = "";
            for (var i = 0; i < skill.SkillId.Count; i++)
            {
                skillsId += skill.SkillId[i];
                if (i < skill.SkillId.Count - 1)
                {
                    skillsId += ",";
                }
            }

            LabelName.ToolTip = skillsId;
            LabelCritRateDmg.Content = stats.CritRateDmg + "%";

            LabelDamagePercentage.Content = stats.DamagePercentage(currentBoss, timedEncounter) + "%";
            LabelTotalDamage.Content = FormatHelpers.Instance.FormatValue(stats.Damage);

            LabelNumberHitDmg.Content = stats.HitsDmg;

            LabelNumberCritDmg.Content = stats.CritsDmg;

            LabelAverageCrit.Content = FormatHelpers.Instance.FormatValue(stats.DmgAverageCrit);
            LabelBiggestCrit.Content = FormatHelpers.Instance.FormatValue(stats.DmgBiggestCrit);
            LabelAverageHit.Content = FormatHelpers.Instance.FormatValue(stats.DmgAverageHit);
            LabelAverageTotal.Content = FormatHelpers.Instance.FormatValue(stats.DmgAverageTotal);

            IEnumerable<KeyValuePair<int, SkillDetailStats>> listStats = stats.SkillDetails.ToList();
            listStats = listStats.OrderByDescending(stat => stat.Value.Damage);
            SkillsDetailList.Items.Clear();
            foreach (var stat in listStats)
            {
                SkillsDetailList.Items.Add(new SkillDetailDps(stat.Value, currentBoss, timedEncounter));
            }
        }

        public string SkillNameIdent()
        {
            return (string) LabelName.Content;
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            var w = Window.GetWindow(this);
            try
            {
                w?.DragMove();
            }
            catch
            {
                Console.WriteLine(@"Exception move");
            }
        }

        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Background = Brushes.Transparent;
        }

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Background = Brushes.Black;
        }
    }
}