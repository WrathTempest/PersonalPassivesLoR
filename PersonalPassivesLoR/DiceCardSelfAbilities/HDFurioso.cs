using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalPassivesLoR.DiceCardSelfAbilities
{
    public class DiceCardSelfAbility_HDFurioso : DiceCardSelfAbilityBase
    {
        public static string Desc = "When this card hits, inflict 5 stacks of Bleeding, Vulnerable, and 3 stacks of Bind, Feeble.";
        public override string[] Keywords
        {
            get
            {
                return new string[] { "Bleeding_Keyword", "Vulnerable_Keyword", "Binding_Keyword", "Weak_Keyword" };
            }
        }

        // Token: 0x0600398C RID: 14732 RVA: 0x0013B688 File Offset: 0x00139888
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            if (this.card != null && this.card.target != null)
            {
                this.card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 5, base.owner);
                this.card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Binding, 3, base.owner);
                this.card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Vulnerable, 5, base.owner);
                this.card.target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Weak, 3, base.owner);
            }
        }
    }
}
