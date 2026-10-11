// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Modified C# port, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e.
// Port of tsc/internal/scanner/unicodeproperties.go.
namespace TypeScript.Scanner;

public sealed partial class Scanner
{
    // Table 66: Non-binary Unicode property aliases and their canonical property names
    // https://tc39.es/ecma262/#table-nonbinary-unicode-properties
    private static readonly Dictionary<ByteStringKey, string> nonBinaryUnicodeProperties = new(ByteStringKey.Comparer)
    {
        [ByteStringKey.FromString("General_Category")] = "General_Category",
        [ByteStringKey.FromString("gc")] = "General_Category",
        [ByteStringKey.FromString("Script")] = "Script",
        [ByteStringKey.FromString("sc")] = "Script",
        [ByteStringKey.FromString("Script_Extensions")] = "Script_Extensions",
        [ByteStringKey.FromString("scx")] = "Script_Extensions",
    };

    // Table 67: Binary Unicode property aliases and their canonical property names
    // https://tc39.es/ecma262/#table-binary-unicode-properties
    private static readonly HashSet<ByteStringKey> binaryUnicodeProperties = new(ByteStringKey.Comparer) {
    ByteStringKey.FromString("ASCII"), ByteStringKey.FromString("ASCII_Hex_Digit"), ByteStringKey.FromString("AHex"), ByteStringKey.FromString("Alphabetic"), ByteStringKey.FromString("Alpha"), ByteStringKey.FromString("Any"), ByteStringKey.FromString("Assigned"),
    ByteStringKey.FromString("Bidi_Control"), ByteStringKey.FromString("Bidi_C"), ByteStringKey.FromString("Bidi_Mirrored"), ByteStringKey.FromString("Bidi_M"),
    ByteStringKey.FromString("Case_Ignorable"), ByteStringKey.FromString("CI"), ByteStringKey.FromString("Cased"),
    ByteStringKey.FromString("Changes_When_Casefolded"), ByteStringKey.FromString("CWCF"), ByteStringKey.FromString("Changes_When_Casemapped"), ByteStringKey.FromString("CWCM"),
    ByteStringKey.FromString("Changes_When_Lowercased"), ByteStringKey.FromString("CWL"), ByteStringKey.FromString("Changes_When_NFKC_Casefolded"), ByteStringKey.FromString("CWKCF"),
    ByteStringKey.FromString("Changes_When_Titlecased"), ByteStringKey.FromString("CWT"), ByteStringKey.FromString("Changes_When_Uppercased"), ByteStringKey.FromString("CWU"),
    ByteStringKey.FromString("Dash"), ByteStringKey.FromString("Default_Ignorable_Code_Point"), ByteStringKey.FromString("DI"), ByteStringKey.FromString("Deprecated"), ByteStringKey.FromString("Dep"),
    ByteStringKey.FromString("Diacritic"), ByteStringKey.FromString("Dia"),
    ByteStringKey.FromString("Emoji"), ByteStringKey.FromString("Emoji_Component"), ByteStringKey.FromString("EComp"), ByteStringKey.FromString("Emoji_Modifier"), ByteStringKey.FromString("EMod"),
    ByteStringKey.FromString("Emoji_Modifier_Base"), ByteStringKey.FromString("EBase"), ByteStringKey.FromString("Emoji_Presentation"), ByteStringKey.FromString("EPres"),
    ByteStringKey.FromString("Extended_Pictographic"), ByteStringKey.FromString("ExtPict"), ByteStringKey.FromString("Extender"), ByteStringKey.FromString("Ext"),
    ByteStringKey.FromString("Grapheme_Base"), ByteStringKey.FromString("Gr_Base"), ByteStringKey.FromString("Grapheme_Extend"), ByteStringKey.FromString("Gr_Ext"),
    ByteStringKey.FromString("Hex_Digit"), ByteStringKey.FromString("Hex"),
    ByteStringKey.FromString("IDS_Binary_Operator"), ByteStringKey.FromString("IDSB"), ByteStringKey.FromString("IDS_Trinary_Operator"), ByteStringKey.FromString("IDST"),
    ByteStringKey.FromString("ID_Continue"), ByteStringKey.FromString("IDC"), ByteStringKey.FromString("ID_Start"), ByteStringKey.FromString("IDS"),
    ByteStringKey.FromString("Ideographic"), ByteStringKey.FromString("Ideo"),
    ByteStringKey.FromString("Join_Control"), ByteStringKey.FromString("Join_C"),
    ByteStringKey.FromString("Logical_Order_Exception"), ByteStringKey.FromString("LOE"),
    ByteStringKey.FromString("Lowercase"), ByteStringKey.FromString("Lower"), ByteStringKey.FromString("Math"),
    ByteStringKey.FromString("Noncharacter_Code_Point"), ByteStringKey.FromString("NChar"),
    ByteStringKey.FromString("Pattern_Syntax"), ByteStringKey.FromString("Pat_Syn"), ByteStringKey.FromString("Pattern_White_Space"), ByteStringKey.FromString("Pat_WS"),
    ByteStringKey.FromString("Quotation_Mark"), ByteStringKey.FromString("QMark"),
    ByteStringKey.FromString("Radical"),
    ByteStringKey.FromString("Regional_Indicator"), ByteStringKey.FromString("RI"),
    ByteStringKey.FromString("Sentence_Terminal"), ByteStringKey.FromString("STerm"),
    ByteStringKey.FromString("Soft_Dotted"), ByteStringKey.FromString("SD"),
    ByteStringKey.FromString("Terminal_Punctuation"), ByteStringKey.FromString("Term"),
    ByteStringKey.FromString("Unified_Ideograph"), ByteStringKey.FromString("UIdeo"),
    ByteStringKey.FromString("Uppercase"), ByteStringKey.FromString("Upper"),
    ByteStringKey.FromString("Variation_Selector"), ByteStringKey.FromString("VS"),
    ByteStringKey.FromString("White_Space"), ByteStringKey.FromString("space"),
    ByteStringKey.FromString("XID_Continue"), ByteStringKey.FromString("XIDC"), ByteStringKey.FromString("XID_Start"), ByteStringKey.FromString("XIDS"),
};

    // Table 68: Binary Unicode properties of strings
    // https://tc39.es/ecma262/#table-binary-unicode-properties-of-strings
    private static readonly HashSet<ByteStringKey> binaryUnicodePropertiesOfStrings = new(ByteStringKey.Comparer) {
    ByteStringKey.FromString("Basic_Emoji"), ByteStringKey.FromString("Emoji_Keycap_Sequence"), ByteStringKey.FromString("RGI_Emoji_Modifier_Sequence"),
    ByteStringKey.FromString("RGI_Emoji_Flag_Sequence"), ByteStringKey.FromString("RGI_Emoji_Tag_Sequence"),
    ByteStringKey.FromString("RGI_Emoji_ZWJ_Sequence"), ByteStringKey.FromString("RGI_Emoji"),
};

    // Unicode 15.1
    private static readonly HashSet<ByteStringKey> scriptValues = new(ByteStringKey.Comparer) {
    ByteStringKey.FromString("Adlm"), ByteStringKey.FromString("Adlam"), ByteStringKey.FromString("Aghb"), ByteStringKey.FromString("Caucasian_Albanian"), ByteStringKey.FromString("Ahom"), ByteStringKey.FromString("Arab"), ByteStringKey.FromString("Arabic"),
    ByteStringKey.FromString("Armi"), ByteStringKey.FromString("Imperial_Aramaic"), ByteStringKey.FromString("Armn"), ByteStringKey.FromString("Armenian"), ByteStringKey.FromString("Avst"), ByteStringKey.FromString("Avestan"),
    ByteStringKey.FromString("Bali"), ByteStringKey.FromString("Balinese"), ByteStringKey.FromString("Bamu"), ByteStringKey.FromString("Bamum"), ByteStringKey.FromString("Bass"), ByteStringKey.FromString("Bassa_Vah"), ByteStringKey.FromString("Batk"), ByteStringKey.FromString("Batak"),
    ByteStringKey.FromString("Beng"), ByteStringKey.FromString("Bengali"), ByteStringKey.FromString("Bhks"), ByteStringKey.FromString("Bhaiksuki"), ByteStringKey.FromString("Bopo"), ByteStringKey.FromString("Bopomofo"), ByteStringKey.FromString("Brah"), ByteStringKey.FromString("Brahmi"),
    ByteStringKey.FromString("Brai"), ByteStringKey.FromString("Braille"), ByteStringKey.FromString("Bugi"), ByteStringKey.FromString("Buginese"), ByteStringKey.FromString("Buhd"), ByteStringKey.FromString("Buhid"),
    ByteStringKey.FromString("Cakm"), ByteStringKey.FromString("Chakma"), ByteStringKey.FromString("Cans"), ByteStringKey.FromString("Canadian_Aboriginal"), ByteStringKey.FromString("Cari"), ByteStringKey.FromString("Carian"),
    ByteStringKey.FromString("Cham"), ByteStringKey.FromString("Cher"), ByteStringKey.FromString("Cherokee"), ByteStringKey.FromString("Chrs"), ByteStringKey.FromString("Chorasmian"),
    ByteStringKey.FromString("Copt"), ByteStringKey.FromString("Coptic"), ByteStringKey.FromString("Qaac"), ByteStringKey.FromString("Cpmn"), ByteStringKey.FromString("Cypro_Minoan"), ByteStringKey.FromString("Cprt"), ByteStringKey.FromString("Cypriot"),
    ByteStringKey.FromString("Cyrl"), ByteStringKey.FromString("Cyrillic"),
    ByteStringKey.FromString("Deva"), ByteStringKey.FromString("Devanagari"), ByteStringKey.FromString("Diak"), ByteStringKey.FromString("Dives_Akuru"), ByteStringKey.FromString("Dogr"), ByteStringKey.FromString("Dogra"),
    ByteStringKey.FromString("Dsrt"), ByteStringKey.FromString("Deseret"), ByteStringKey.FromString("Dupl"), ByteStringKey.FromString("Duployan"),
    ByteStringKey.FromString("Egyp"), ByteStringKey.FromString("Egyptian_Hieroglyphs"), ByteStringKey.FromString("Elba"), ByteStringKey.FromString("Elbasan"), ByteStringKey.FromString("Elym"), ByteStringKey.FromString("Elymaic"),
    ByteStringKey.FromString("Ethi"), ByteStringKey.FromString("Ethiopic"),
    ByteStringKey.FromString("Geor"), ByteStringKey.FromString("Georgian"), ByteStringKey.FromString("Glag"), ByteStringKey.FromString("Glagolitic"),
    ByteStringKey.FromString("Gong"), ByteStringKey.FromString("Gunjala_Gondi"), ByteStringKey.FromString("Gonm"), ByteStringKey.FromString("Masaram_Gondi"),
    ByteStringKey.FromString("Goth"), ByteStringKey.FromString("Gothic"), ByteStringKey.FromString("Gran"), ByteStringKey.FromString("Grantha"), ByteStringKey.FromString("Grek"), ByteStringKey.FromString("Greek"),
    ByteStringKey.FromString("Gujr"), ByteStringKey.FromString("Gujarati"), ByteStringKey.FromString("Guru"), ByteStringKey.FromString("Gurmukhi"),
    ByteStringKey.FromString("Hang"), ByteStringKey.FromString("Hangul"), ByteStringKey.FromString("Hani"), ByteStringKey.FromString("Han"), ByteStringKey.FromString("Hano"), ByteStringKey.FromString("Hanunoo"),
    ByteStringKey.FromString("Hatr"), ByteStringKey.FromString("Hatran"), ByteStringKey.FromString("Hebr"), ByteStringKey.FromString("Hebrew"),
    ByteStringKey.FromString("Hira"), ByteStringKey.FromString("Hiragana"), ByteStringKey.FromString("Hluw"), ByteStringKey.FromString("Anatolian_Hieroglyphs"),
    ByteStringKey.FromString("Hmng"), ByteStringKey.FromString("Pahawh_Hmong"), ByteStringKey.FromString("Hmnp"), ByteStringKey.FromString("Nyiakeng_Puachue_Hmong"),
    ByteStringKey.FromString("Hrkt"), ByteStringKey.FromString("Katakana_Or_Hiragana"),
    ByteStringKey.FromString("Hung"), ByteStringKey.FromString("Old_Hungarian"),
    ByteStringKey.FromString("Ital"), ByteStringKey.FromString("Old_Italic"),
    ByteStringKey.FromString("Java"), ByteStringKey.FromString("Javanese"),
    ByteStringKey.FromString("Kali"), ByteStringKey.FromString("Kayah_Li"), ByteStringKey.FromString("Kana"), ByteStringKey.FromString("Katakana"), ByteStringKey.FromString("Kawi"),
    ByteStringKey.FromString("Khar"), ByteStringKey.FromString("Kharoshthi"), ByteStringKey.FromString("Khmr"), ByteStringKey.FromString("Khmer"), ByteStringKey.FromString("Khoj"), ByteStringKey.FromString("Khojki"),
    ByteStringKey.FromString("Kits"), ByteStringKey.FromString("Khitan_Small_Script"), ByteStringKey.FromString("Knda"), ByteStringKey.FromString("Kannada"), ByteStringKey.FromString("Kthi"), ByteStringKey.FromString("Kaithi"),
    ByteStringKey.FromString("Lana"), ByteStringKey.FromString("Tai_Tham"), ByteStringKey.FromString("Laoo"), ByteStringKey.FromString("Lao"), ByteStringKey.FromString("Latn"), ByteStringKey.FromString("Latin"),
    ByteStringKey.FromString("Lepc"), ByteStringKey.FromString("Lepcha"), ByteStringKey.FromString("Limb"), ByteStringKey.FromString("Limbu"),
    ByteStringKey.FromString("Lina"), ByteStringKey.FromString("Linear_A"), ByteStringKey.FromString("Linb"), ByteStringKey.FromString("Linear_B"), ByteStringKey.FromString("Lisu"),
    ByteStringKey.FromString("Lyci"), ByteStringKey.FromString("Lycian"), ByteStringKey.FromString("Lydi"), ByteStringKey.FromString("Lydian"),
    ByteStringKey.FromString("Mahj"), ByteStringKey.FromString("Mahajani"), ByteStringKey.FromString("Maka"), ByteStringKey.FromString("Makasar"),
    ByteStringKey.FromString("Mand"), ByteStringKey.FromString("Mandaic"), ByteStringKey.FromString("Mani"), ByteStringKey.FromString("Manichaean"), ByteStringKey.FromString("Marc"), ByteStringKey.FromString("Marchen"),
    ByteStringKey.FromString("Medf"), ByteStringKey.FromString("Medefaidrin"), ByteStringKey.FromString("Mend"), ByteStringKey.FromString("Mende_Kikakui"),
    ByteStringKey.FromString("Merc"), ByteStringKey.FromString("Meroitic_Cursive"), ByteStringKey.FromString("Mero"), ByteStringKey.FromString("Meroitic_Hieroglyphs"),
    ByteStringKey.FromString("Mlym"), ByteStringKey.FromString("Malayalam"), ByteStringKey.FromString("Modi"), ByteStringKey.FromString("Mong"), ByteStringKey.FromString("Mongolian"),
    ByteStringKey.FromString("Mroo"), ByteStringKey.FromString("Mro"), ByteStringKey.FromString("Mtei"), ByteStringKey.FromString("Meetei_Mayek"), ByteStringKey.FromString("Mult"), ByteStringKey.FromString("Multani"),
    ByteStringKey.FromString("Mymr"), ByteStringKey.FromString("Myanmar"),
    ByteStringKey.FromString("Nagm"), ByteStringKey.FromString("Nag_Mundari"), ByteStringKey.FromString("Nand"), ByteStringKey.FromString("Nandinagari"),
    ByteStringKey.FromString("Narb"), ByteStringKey.FromString("Old_North_Arabian"), ByteStringKey.FromString("Nbat"), ByteStringKey.FromString("Nabataean"),
    ByteStringKey.FromString("Newa"), ByteStringKey.FromString("Nkoo"), ByteStringKey.FromString("Nko"), ByteStringKey.FromString("Nshu"), ByteStringKey.FromString("Nushu"),
    ByteStringKey.FromString("Ogam"), ByteStringKey.FromString("Ogham"), ByteStringKey.FromString("Olck"), ByteStringKey.FromString("Ol_Chiki"),
    ByteStringKey.FromString("Orkh"), ByteStringKey.FromString("Old_Turkic"), ByteStringKey.FromString("Orya"), ByteStringKey.FromString("Oriya"),
    ByteStringKey.FromString("Osge"), ByteStringKey.FromString("Osage"), ByteStringKey.FromString("Osma"), ByteStringKey.FromString("Osmanya"), ByteStringKey.FromString("Ougr"), ByteStringKey.FromString("Old_Uyghur"),
    ByteStringKey.FromString("Palm"), ByteStringKey.FromString("Palmyrene"), ByteStringKey.FromString("Pauc"), ByteStringKey.FromString("Pau_Cin_Hau"),
    ByteStringKey.FromString("Perm"), ByteStringKey.FromString("Old_Permic"), ByteStringKey.FromString("Phag"), ByteStringKey.FromString("Phags_Pa"),
    ByteStringKey.FromString("Phli"), ByteStringKey.FromString("Inscriptional_Pahlavi"), ByteStringKey.FromString("Phlp"), ByteStringKey.FromString("Psalter_Pahlavi"),
    ByteStringKey.FromString("Phnx"), ByteStringKey.FromString("Phoenician"), ByteStringKey.FromString("Plrd"), ByteStringKey.FromString("Miao"),
    ByteStringKey.FromString("Prti"), ByteStringKey.FromString("Inscriptional_Parthian"),
    ByteStringKey.FromString("Rjng"), ByteStringKey.FromString("Rejang"), ByteStringKey.FromString("Rohg"), ByteStringKey.FromString("Hanifi_Rohingya"),
    ByteStringKey.FromString("Runr"), ByteStringKey.FromString("Runic"),
    ByteStringKey.FromString("Samr"), ByteStringKey.FromString("Samaritan"), ByteStringKey.FromString("Sarb"), ByteStringKey.FromString("Old_South_Arabian"),
    ByteStringKey.FromString("Saur"), ByteStringKey.FromString("Saurashtra"), ByteStringKey.FromString("Sgnw"), ByteStringKey.FromString("SignWriting"),
    ByteStringKey.FromString("Shaw"), ByteStringKey.FromString("Shavian"), ByteStringKey.FromString("Shrd"), ByteStringKey.FromString("Sharada"),
    ByteStringKey.FromString("Sidd"), ByteStringKey.FromString("Siddham"), ByteStringKey.FromString("Sind"), ByteStringKey.FromString("Khudawadi"), ByteStringKey.FromString("Sinh"), ByteStringKey.FromString("Sinhala"),
    ByteStringKey.FromString("Sogd"), ByteStringKey.FromString("Sogdian"), ByteStringKey.FromString("Sogo"), ByteStringKey.FromString("Old_Sogdian"),
    ByteStringKey.FromString("Sora"), ByteStringKey.FromString("Sora_Sompeng"), ByteStringKey.FromString("Soyo"), ByteStringKey.FromString("Soyombo"),
    ByteStringKey.FromString("Sund"), ByteStringKey.FromString("Sundanese"), ByteStringKey.FromString("Sylo"), ByteStringKey.FromString("Syloti_Nagri"), ByteStringKey.FromString("Syrc"), ByteStringKey.FromString("Syriac"),
    ByteStringKey.FromString("Tagb"), ByteStringKey.FromString("Tagbanwa"), ByteStringKey.FromString("Takr"), ByteStringKey.FromString("Takri"),
    ByteStringKey.FromString("Tale"), ByteStringKey.FromString("Tai_Le"), ByteStringKey.FromString("Talu"), ByteStringKey.FromString("New_Tai_Lue"),
    ByteStringKey.FromString("Taml"), ByteStringKey.FromString("Tamil"), ByteStringKey.FromString("Tang"), ByteStringKey.FromString("Tangut"), ByteStringKey.FromString("Tavt"), ByteStringKey.FromString("Tai_Viet"),
    ByteStringKey.FromString("Telu"), ByteStringKey.FromString("Telugu"), ByteStringKey.FromString("Tfng"), ByteStringKey.FromString("Tifinagh"),
    ByteStringKey.FromString("Tglg"), ByteStringKey.FromString("Tagalog"), ByteStringKey.FromString("Thaa"), ByteStringKey.FromString("Thaana"), ByteStringKey.FromString("Thai"), ByteStringKey.FromString("Tibt"), ByteStringKey.FromString("Tibetan"),
    ByteStringKey.FromString("Tirh"), ByteStringKey.FromString("Tirhuta"), ByteStringKey.FromString("Tnsa"), ByteStringKey.FromString("Tangsa"), ByteStringKey.FromString("Toto"),
    ByteStringKey.FromString("Ugar"), ByteStringKey.FromString("Ugaritic"),
    ByteStringKey.FromString("Vaii"), ByteStringKey.FromString("Vai"), ByteStringKey.FromString("Vith"), ByteStringKey.FromString("Vithkuqi"),
    ByteStringKey.FromString("Wara"), ByteStringKey.FromString("Warang_Citi"), ByteStringKey.FromString("Wcho"), ByteStringKey.FromString("Wancho"),
    ByteStringKey.FromString("Xpeo"), ByteStringKey.FromString("Old_Persian"), ByteStringKey.FromString("Xsux"), ByteStringKey.FromString("Cuneiform"),
    ByteStringKey.FromString("Yezi"), ByteStringKey.FromString("Yezidi"), ByteStringKey.FromString("Yiii"), ByteStringKey.FromString("Yi"),
    ByteStringKey.FromString("Zanb"), ByteStringKey.FromString("Zanabazar_Square"),
    ByteStringKey.FromString("Zinh"), ByteStringKey.FromString("Inherited"), ByteStringKey.FromString("Qaai"),
    ByteStringKey.FromString("Zyyy"), ByteStringKey.FromString("Common"),
    ByteStringKey.FromString("Zzzz"), ByteStringKey.FromString("Unknown"),
};

    private static readonly Dictionary<ByteStringKey, HashSet<ByteStringKey>> valuesOfNonBinaryUnicodeProperties = new(ByteStringKey.Comparer)
    {
        [ByteStringKey.FromString("General_Category")] = new HashSet<ByteStringKey>(ByteStringKey.Comparer) {
        ByteStringKey.FromString("C"), ByteStringKey.FromString("Other"), ByteStringKey.FromString("Cc"), ByteStringKey.FromString("Control"), ByteStringKey.FromString("cntrl"), ByteStringKey.FromString("Cf"), ByteStringKey.FromString("Format"), ByteStringKey.FromString("Cn"), ByteStringKey.FromString("Unassigned"),
        ByteStringKey.FromString("Co"), ByteStringKey.FromString("Private_Use"), ByteStringKey.FromString("Cs"), ByteStringKey.FromString("Surrogate"),
        ByteStringKey.FromString("L"), ByteStringKey.FromString("Letter"), ByteStringKey.FromString("LC"), ByteStringKey.FromString("Cased_Letter"), ByteStringKey.FromString("Ll"), ByteStringKey.FromString("Lowercase_Letter"), ByteStringKey.FromString("Lm"), ByteStringKey.FromString("Modifier_Letter"),
        ByteStringKey.FromString("Lo"), ByteStringKey.FromString("Other_Letter"), ByteStringKey.FromString("Lt"), ByteStringKey.FromString("Titlecase_Letter"), ByteStringKey.FromString("Lu"), ByteStringKey.FromString("Uppercase_Letter"),
        ByteStringKey.FromString("M"), ByteStringKey.FromString("Mark"), ByteStringKey.FromString("Combining_Mark"), ByteStringKey.FromString("Mc"), ByteStringKey.FromString("Spacing_Mark"), ByteStringKey.FromString("Me"), ByteStringKey.FromString("Enclosing_Mark"),
        ByteStringKey.FromString("Mn"), ByteStringKey.FromString("Nonspacing_Mark"),
        ByteStringKey.FromString("N"), ByteStringKey.FromString("Number"), ByteStringKey.FromString("Nd"), ByteStringKey.FromString("Decimal_Number"), ByteStringKey.FromString("digit"), ByteStringKey.FromString("Nl"), ByteStringKey.FromString("Letter_Number"), ByteStringKey.FromString("No"), ByteStringKey.FromString("Other_Number"),
        ByteStringKey.FromString("P"), ByteStringKey.FromString("Punctuation"), ByteStringKey.FromString("punct"), ByteStringKey.FromString("Pc"), ByteStringKey.FromString("Connector_Punctuation"), ByteStringKey.FromString("Pd"), ByteStringKey.FromString("Dash_Punctuation"),
        ByteStringKey.FromString("Pe"), ByteStringKey.FromString("Close_Punctuation"), ByteStringKey.FromString("Pf"), ByteStringKey.FromString("Final_Punctuation"), ByteStringKey.FromString("Pi"), ByteStringKey.FromString("Initial_Punctuation"),
        ByteStringKey.FromString("Po"), ByteStringKey.FromString("Other_Punctuation"), ByteStringKey.FromString("Ps"), ByteStringKey.FromString("Open_Punctuation"),
        ByteStringKey.FromString("S"), ByteStringKey.FromString("Symbol"), ByteStringKey.FromString("Sc"), ByteStringKey.FromString("Currency_Symbol"), ByteStringKey.FromString("Sk"), ByteStringKey.FromString("Modifier_Symbol"),
        ByteStringKey.FromString("Sm"), ByteStringKey.FromString("Math_Symbol"), ByteStringKey.FromString("So"), ByteStringKey.FromString("Other_Symbol"),
        ByteStringKey.FromString("Z"), ByteStringKey.FromString("Separator"), ByteStringKey.FromString("Zl"), ByteStringKey.FromString("Line_Separator"), ByteStringKey.FromString("Zp"), ByteStringKey.FromString("Paragraph_Separator"),
        ByteStringKey.FromString("Zs"), ByteStringKey.FromString("Space_Separator"),
    },
        [ByteStringKey.FromString("Script")] = scriptValues,
        // The Script_Extensions property of a character contains one or more Script values.
        // See https://www.unicode.org/reports/tr24/#Script_Extensions
        // Here, since each Unicode property value expression only allows a single value,
        // its values can be considered the same as those of the Script property.
        [ByteStringKey.FromString("Script_Extensions")] = scriptValues,
    };
}
