using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Gizmo that appears on chairs to designate their courtroom role
    /// </summary>
    public class Gizmo_DesignateChairRole : Gizmo
    {
        private Thing chair;
        private CompCourtroomChair comp;

        public Gizmo_DesignateChairRole(Thing chair, CompCourtroomChair comp)
        {
            this.chair = chair;
            this.comp = comp;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

            bool mouseOver = false;
            if (Mouse.IsOver(rect))
            {
                mouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }

            // Background
            Widgets.DrawWindowBackground(rect);
            GUI.color = Color.white;

            // Icon
            Rect iconRect = new Rect(rect.x + 4f, rect.y + 4f, 32f, 32f);
            GUI.DrawTexture(iconRect, TexCommand.GatherSpotActive);

            // Label
            Text.Font = GameFont.Tiny;
            Rect labelRect = new Rect(iconRect.xMax + 4f, rect.y + 4f, rect.width - iconRect.width - 12f, 18f);
            Widgets.Label(labelRect, "Courtroom Role");

            // Current role display
            Text.Font = GameFont.Tiny;
            Rect roleRect = new Rect(iconRect.xMax + 4f, labelRect.yMax, rect.width - iconRect.width - 12f, 18f);
            string roleText = comp.Role == CourtroomChairRole.Unassigned
                ? "(Unassigned)"
                : comp.Role.ToString();
            Widgets.Label(roleRect, roleText);

            // Click to open menu
            if (Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                OpenRoleSelectionMenu();
                return new GizmoResult(GizmoState.Interacted);
            }

            // Tooltip
            if (mouseOver)
            {
                string tooltip = "Designate this chair for a specific courtroom role.\n\n" +
                               "Roles:\n" +
                               "• Judge - For the adjudicator\n" +
                               "• Jury - For jury members\n" +
                               "• Defendant - For the accused\n" +
                               "• Victim - For crime victims\n" +
                               "• Spectator - For observers";
                TooltipHandler.TipRegion(rect, tooltip);
            }

            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }

        private void OpenRoleSelectionMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // Unassigned option
            options.Add(new FloatMenuOption(
                "Unassigned",
                () => SetRole(CourtroomChairRole.Unassigned),
                MenuOptionPriority.Default
            ));

            // Judge option
            options.Add(new FloatMenuOption(
                "Judge Seat",
                () => SetRole(CourtroomChairRole.Judge),
                MenuOptionPriority.Default
            ));

            // Jury option
            options.Add(new FloatMenuOption(
                "Jury Seat",
                () => SetRole(CourtroomChairRole.Jury),
                MenuOptionPriority.Default
            ));

            // Defendant option
            options.Add(new FloatMenuOption(
                "Defendant Seat",
                () => SetRole(CourtroomChairRole.Defendant),
                MenuOptionPriority.Default
            ));

            // Victim option
            options.Add(new FloatMenuOption(
                "Victim Seat",
                () => SetRole(CourtroomChairRole.Victim),
                MenuOptionPriority.Default
            ));

            // Spectator option
            options.Add(new FloatMenuOption(
                "Spectator Seat",
                () => SetRole(CourtroomChairRole.Spectator),
                MenuOptionPriority.Default
            ));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void SetRole(CourtroomChairRole newRole)
        {
            comp.Role = newRole;

            // Play appropriate sound
            if (newRole == CourtroomChairRole.Unassigned)
            {
                SoundDefOf.Designate_Cancel.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.Designate_Claim.PlayOneShotOnCamera();
            }

            // Show confirmation message
            if (newRole != CourtroomChairRole.Unassigned)
            {
                Messages.Message(
                    $"{chair.Label} designated as {newRole} seat.",
                    chair,
                    MessageTypeDefOf.TaskCompletion,
                    false
                );
            }
        }
    }
}
