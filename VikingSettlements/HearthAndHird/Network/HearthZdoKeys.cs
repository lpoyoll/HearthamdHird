namespace HearthAndHird.Network
{
    /// <summary>
    /// Network keys owned by Hearth &amp; Hird. The existing vs_* keys remain
    /// untouched so worlds created with VikingSettlements continue to load.
    /// </summary>
    internal static class HearthZdoKeys
    {
        internal const string ProfileVersion = "hnh_profile_version";
        internal const string Sex = "hnh_sex";
        internal const string HairStyle = "hnh_hair_style";
        internal const string BeardStyle = "hnh_beard_style";
        internal const string SkinTone = "hnh_skin_tone";
        internal const string HairTone = "hnh_hair_tone";
        internal const string Health = "hnh_attr_health";
        internal const string Stamina = "hnh_attr_stamina";
        internal const string Strength = "hnh_attr_strength";
        internal const string Agility = "hnh_attr_agility";
        internal const string Courage = "hnh_attr_courage";
        internal const string WorkEthic = "hnh_attr_work_ethic";
        internal const string Loyalty = "hnh_attr_loyalty";

        internal const string Directive = "hnh_directive";
        internal const string DirectiveRevision = "hnh_directive_revision";
        internal const string DirectiveTarget = "hnh_directive_target";
        internal const string DirectiveWorkId = "hnh_directive_work";
        internal const string DirectiveIssuer = "hnh_directive_issuer";

        internal const string HirdCombatStance = "hnh_hird_combat_stance";
        internal const string HirdFormation = "hnh_hird_formation";
    }
}
