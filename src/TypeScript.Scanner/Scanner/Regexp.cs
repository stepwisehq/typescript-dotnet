// Copyright (c) Microsoft Corporation. Licensed under the Apache License 2.0.
// See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/scanner/regexp.go; modified C# translation, upstream fed0bf24149fb1ed36039212648bafdafc1ea10e; see UPSTREAM.md.
using System;
using System.Collections.Generic;
using System.Linq;
namespace TypeScript.Scanner;

public partial class Scanner
{
    [Flags]
    private enum regularExpressionFlags
    {
        None = 0, HasIndices = 1 << 0, Global = 1 << 1, IgnoreCase = 1 << 2, Multiline = 1 << 3,
        DotAll = 1 << 4, Unicode = 1 << 5, UnicodeSets = 1 << 6, Sticky = 1 << 7,
        AnyUnicodeMode = Unicode | UnicodeSets, Modifiers = IgnoreCase | Multiline | DotAll
    }
    private enum classSetExpressionType { Unknown, ClassUnion, ClassIntersection, ClassSubtraction }
    private static readonly Dictionary<int, regularExpressionFlags> charCodeToRegExpFlag = new()
    {
        ['d'] = regularExpressionFlags.HasIndices,
        ['g'] = regularExpressionFlags.Global,
        ['i'] = regularExpressionFlags.IgnoreCase,
        ['m'] = regularExpressionFlags.Multiline,
        ['s'] = regularExpressionFlags.DotAll,
        ['u'] = regularExpressionFlags.Unicode,
        ['v'] = regularExpressionFlags.UnicodeSets,
        ['y'] = regularExpressionFlags.Sticky
    };
    private static readonly Dictionary<regularExpressionFlags, ScriptTarget> regExpFlagToFirstAvailableLanguageVersion = new()
    {
        [regularExpressionFlags.HasIndices] = ScriptTarget.ES2022,
        [regularExpressionFlags.DotAll] = ScriptTarget.ES2018,
        [regularExpressionFlags.UnicodeSets] = ScriptTarget.ES2024
    };
    private static string ToLower(string s) => s.ToLowerInvariant();

    private void checkRegularExpressionFlagAvailability(regularExpressionFlags flag, int pos, int size)
    {
        {
            var ok = regExpFlagToFirstAvailableLanguageVersion.TryGetValue(flag, out var availableFrom);
            if ((ok && (this.languageVersion() < availableFrom)))
            {
                this.errorAt(Diagnostics.This_regular_expression_flag_is_only_available_when_targeting_0_or_later, pos, size, ToLower(availableFrom.ToString()));
            }
        }
    }

    private sealed class regExpParser
    {
        public Scanner scanner = null!;
        public int end;
        public regularExpressionFlags regExpFlags;
        public bool anyUnicodeMode, unicodeSetsMode, annexB;
        private bool anyUnicodeModeOrNonAnnexB;
        public bool namedCaptureGroups;
        private bool mayContainStrings;
        private int numberOfCapturingGroups;
        public Dictionary<ByteStringKey, bool> groupSpecifiers = new(ByteStringKey.Comparer);
        private Slice<groupNameReference> groupNameReferences;
        private Slice<decimalEscapeValue> decimalEscapes;
        private Slice<Dictionary<ByteStringKey, bool>> namedCapturingGroups;
        private int pendingLowSurrogate;
        private record struct groupNameReference(int pos, int end, ByteStringValue name);
        private record struct decimalEscapeValue(int pos, int end, long value);

        private int pos()
        {
            return this.scanner.pos;
        }

        private void setPos(int v)
        {
            this.scanner.pos = v;
        }

        private void incPos(int n)
        {
            this.scanner.pos += n;
        }

        private int charCode()
        {
            return this.scanner.charCode();
        }

        private int charAt(int pos)
        {
            return this.scanner.charAt((pos - this.pos()));
        }

        private void error(Message msg, int pos, int length, params object[] args)
        {
            this.scanner.errorAt(msg, pos, length, args);
        }

        private ByteStringView text()
        {
            return this.scanner.text;
        }

        private static int compareDecimalStrings(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            a = a.TrimStart((byte)'0');
            b = b.TrimStart((byte)'0');
            if ((a.IsEmpty))
            {
                a = "0"u8;
            }
            if ((b.IsEmpty))
            {
                b = "0"u8;
            }
            if ((a.Length != b.Length))
            {
                if ((a.Length < b.Length))
                {
                    return -1;
                }
                return 1;
            }
            return a.SequenceCompareTo(b);
        }

        // Disjunction ::= Alternative ('|' Alternative)*
        private void scanDisjunction(bool isInGroup)
        {
            HashSet<ByteStringKey> disjunctionNames = new(ByteStringKey.Comparer);
            while (true)
            {
                this.namedCapturingGroups = this.namedCapturingGroups.Append(new Dictionary<ByteStringKey, bool>(ByteStringKey.Comparer));
                this.scanAlternative(isInGroup);
                var alternativeNames = this.namedCapturingGroups[(this.namedCapturingGroups.Count - 1)];
                this.namedCapturingGroups = this.namedCapturingGroups.Slice2(0, this.namedCapturingGroups.Length - 1);
                foreach (var name in alternativeNames.Keys)
                {
                    disjunctionNames.Add(name);
                }
                if ((this.charCode() != '|'))
                {
                    break;
                }
                this.incPos(1);
            }
            if ((isInGroup && (this.namedCapturingGroups.Count > 0)))
            {
                var parentScope = this.namedCapturingGroups[(this.namedCapturingGroups.Count - 1)];
                foreach (var name in disjunctionNames)
                {
                    parentScope[name] = true;
                }
            }
        }

        // Alternative ::= Term*
        // Term ::=
        //
        //	| Assertion
        //	| Atom Quantifier?
        //
        // Assertion ::=
        //
        //	| '^'
        //	| '$'
        //	| '\b'
        //	| '\B'
        //	| '(?=' Disjunction ')'
        //	| '(?!' Disjunction ')'
        //	| '(?<=' Disjunction ')'
        //	| '(?<!' Disjunction ')'
        //
        // Quantifier ::= QuantifierPrefix '?'?
        // QuantifierPrefix ::=
        //
        //	| '*'
        //	| '+'
        //	| '?'
        //	| '{' DecimalDigits (',' DecimalDigits?)? '}'
        //
        // Atom ::=
        //
        //	| PatternCharacter
        //	| '.'
        //	| '\' AtomEscape
        //	| CharacterClass
        //	| '(?<' RegExpIdentifierName '>' Disjunction ')'
        //	| '(?' RegularExpressionFlags ('-' RegularExpressionFlags)? ':' Disjunction ')'
        //
        // CharacterClass ::= unicodeMode
        //
        //	? '[' ClassRanges ']'
        //	: '[' ClassSetExpression ']'
        private void scanAlternative(bool isInGroup)
        {
            var isPreviousTermQuantifiable = false;
            while ((this.pos() < this.end))
            {
                var start = this.pos();
                var ch = this.charCode();
                switch (ch)
                {
                    case '^':
                    case '$':
                        {
                            this.incPos(1);
                            isPreviousTermQuantifiable = false;
                            break;
                        }
                    case '\\':
                        {
                            this.incPos(1);
                            switch (this.charCode())
                            {
                                case 'b':
                                case 'B':
                                    {
                                        this.incPos(1);
                                        isPreviousTermQuantifiable = false;
                                        break;
                                    }
                                default:
                                    {
                                        this.scanAtomEscape();
                                        isPreviousTermQuantifiable = true;
                                        break;
                                    }
                            }
                            break;
                        }
                    case '(':
                        {
                            this.incPos(1);
                            if ((this.charCode() == '?'))
                            {
                                this.incPos(1);
                                switch (this.charCode())
                                {
                                    case '=':
                                    case '!':
                                        {
                                            this.incPos(1);
                                            isPreviousTermQuantifiable = !this.anyUnicodeModeOrNonAnnexB;
                                            break;
                                        }
                                    case '<':
                                        {
                                            var groupNameStart = this.pos();
                                            this.incPos(1);
                                            switch (this.charCode())
                                            {
                                                case '=':
                                                case '!':
                                                    {
                                                        this.incPos(1);
                                                        isPreviousTermQuantifiable = false;
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        this.scanGroupName(false);
                                                        this.scanExpectedChar('>');
                                                        if ((this.scanner.languageVersion() < ScriptTarget.ES2018))
                                                        {
                                                            this.error(Diagnostics.Named_capturing_groups_are_only_available_when_targeting_ES2018_or_later, groupNameStart, (this.pos() - groupNameStart));
                                                        }
                                                        this.numberOfCapturingGroups++;
                                                        isPreviousTermQuantifiable = true;
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                    default:
                                        {
                                            var flagsStart = this.pos();
                                            var setFlags = this.scanPatternModifiers(regularExpressionFlags.None);
                                            if ((this.charCode() == '-'))
                                            {
                                                this.incPos(1);
                                                this.scanPatternModifiers(setFlags);
                                                if ((this.pos() == (flagsStart + 1)))
                                                {
                                                    this.error(Diagnostics.Subpattern_flags_must_be_present_when_there_is_a_minus_sign, flagsStart, (this.pos() - flagsStart));
                                                }
                                            }
                                            if (((this.pos() != flagsStart) && (this.scanner.languageVersion() < ScriptTarget.ES2025)))
                                            {
                                                this.error(Diagnostics.Regular_expression_pattern_modifiers_are_only_available_when_targeting_0_or_later, flagsStart, (this.pos() - flagsStart), ToLower(ScriptTarget.ES2025.ToString()));
                                            }
                                            this.scanExpectedChar(':');
                                            isPreviousTermQuantifiable = true;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                this.numberOfCapturingGroups++;
                                isPreviousTermQuantifiable = true;
                            }
                            this.scanDisjunction(true);
                            this.scanExpectedChar(')');
                            break;
                        }
                    case '{':
                        {
                            this.incPos(1);
                            var digitsStart = this.pos();
                            this.scanDigits();
                            var minStr = this.scanner.TokenValueTextValue();
                            if ((!this.anyUnicodeModeOrNonAnnexB && (minStr.Length == 0)))
                            {
                                isPreviousTermQuantifiable = true;
                                continue;
                            }
                            if ((this.charCode() == ','))
                            {
                                this.incPos(1);
                                this.scanDigits();
                                var maxStr = this.scanner.TokenValueTextValue();
                                if ((minStr.Length == 0))
                                {
                                    if (((maxStr.Length != 0) || (this.charCode() == '}')))
                                    {
                                        this.error(Diagnostics.Incomplete_quantifier_Digit_expected, digitsStart, 0);
                                    }
                                    else
                                    {
                                        this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, start, 1, GoString.FromRune(ch));
                                        isPreviousTermQuantifiable = true;
                                        continue;
                                    }
                                }
                                else
                                {
                                    if ((maxStr.Length != 0))
                                    {
                                        if (((compareDecimalStrings(minStr.ByteSpan, maxStr.ByteSpan) > 0) && ((this.anyUnicodeModeOrNonAnnexB || (this.charCode() == '}')))))
                                        {
                                            this.error(Diagnostics.Numbers_out_of_order_in_quantifier, digitsStart, (this.pos() - digitsStart));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((minStr.Length == 0))
                                {
                                    if (this.anyUnicodeModeOrNonAnnexB)
                                    {
                                        this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, start, 1, GoString.FromRune(ch));
                                    }
                                    isPreviousTermQuantifiable = true;
                                    continue;
                                }
                            }
                            if ((this.charCode() != '}'))
                            {
                                if (this.anyUnicodeModeOrNonAnnexB)
                                {
                                    this.error(Diagnostics.X_0_expected, this.pos(), 0, "}");
                                    this.incPos(-1);
                                }
                                else
                                {
                                    isPreviousTermQuantifiable = true;
                                    continue;
                                }
                            }
                            goto case '*';
                        }
                    case '*':
                    case '+':
                    case '?':
                        {
                            this.incPos(1);
                            if ((this.charCode() == '?'))
                            {
                                this.incPos(1);
                            }
                            if (!isPreviousTermQuantifiable)
                            {
                                this.error(Diagnostics.There_is_nothing_available_for_repetition, start, (this.pos() - start));
                            }
                            isPreviousTermQuantifiable = false;
                            break;
                        }
                    case '.':
                        {
                            this.incPos(1);
                            isPreviousTermQuantifiable = true;
                            break;
                        }
                    case '[':
                        {
                            this.incPos(1);
                            if (this.unicodeSetsMode)
                            {
                                this.scanClassSetExpression();
                            }
                            else
                            {
                                this.scanClassRanges();
                                this.pendingLowSurrogate = 0;
                            }
                            this.scanExpectedChar(']');
                            isPreviousTermQuantifiable = true;
                            break;
                        }
                    case ')':
                        {
                            if (isInGroup)
                            {
                                return;
                            }
                            goto case ']';
                        }
                    case ']':
                    case '}':
                        {
                            if ((this.anyUnicodeModeOrNonAnnexB || (ch == ')')))
                            {
                                this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, this.pos(), 1, GoString.FromRune(ch));
                            }
                            this.incPos(1);
                            isPreviousTermQuantifiable = true;
                            break;
                        }
                    case '/':
                    case '|':
                        {
                            return;
                        }
                    default:
                        {
                            this.scanSourceCharacter();
                            isPreviousTermQuantifiable = true;
                            break;
                        }
                }
            }
        }

        private regularExpressionFlags scanPatternModifiers(regularExpressionFlags currFlags)
        {
            while ((this.pos() < this.end))
            {
                var (ch, size) = Utf8.DecodeRuneInString(this.text().AsSpan(this.pos()));
                if (((ch == Utf8.RuneError) || !IsIdentifierPart(ch)))
                {
                    break;
                }
                var ok = charCodeToRegExpFlag.TryGetValue(ch, out var flag);
                if (!ok)
                {
                    this.error(Diagnostics.Unknown_regular_expression_flag, this.pos(), size);
                }
                else
                {
                    if (((currFlags & flag) != 0))
                    {
                        this.error(Diagnostics.Duplicate_regular_expression_flag, this.pos(), size);
                    }
                    else
                    {
                        if (((flag & regularExpressionFlags.Modifiers) == 0))
                        {
                            this.error(Diagnostics.This_regular_expression_flag_cannot_be_toggled_within_a_subpattern, this.pos(), size);
                        }
                        else
                        {
                            currFlags |= flag;
                        }
                    }
                }
                this.incPos(size);
            }
            return currFlags;
        }

        // AtomEscape ::=
        //
        //	| DecimalEscape
        //	| CharacterClassEscape
        //	| CharacterEscape
        //	| 'k<' RegExpIdentifierName '>'
        private void scanAtomEscape()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '\\')));
            switch (this.charCode())
            {
                case 'k':
                    {
                        this.incPos(1);
                        if ((this.charCode() == '<'))
                        {
                            this.incPos(1);
                            this.scanGroupName(true);
                            this.scanExpectedChar('>');
                        }
                        else
                        {
                            if ((this.anyUnicodeModeOrNonAnnexB || this.namedCaptureGroups))
                            {
                                this.error(Diagnostics.X_k_must_be_followed_by_a_capturing_group_name_enclosed_in_angle_brackets, (this.pos() - 2), 2);
                            }
                        }
                        break;
                    }
                case 'q':
                    {
                        if (this.unicodeSetsMode)
                        {
                            this.incPos(1);
                            this.error(Diagnostics.X_q_is_only_available_inside_character_class, (this.pos() - 2), 2);
                            return;
                        }
                        goto default;
                    }
                default:
                    {
                        if ((!this.scanCharacterClassEscape() && !this.scanDecimalEscape()))
                        {
                            Debug.Assert((this.scanCharacterEscape(true).Length != 0));
                        }
                        break;
                    }
            }
        }

        // DecimalEscape ::= [1-9] [0-9]*
        private bool scanDecimalEscape()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '\\')));
            var ch = this.charCode();
            if (((ch >= '1') && (ch <= '9')))
            {
                var start = this.pos();
                this.scanDigits();
                bool valid = long.TryParse(this.scanner.TokenValueTextValue().ByteSpan, out var val);
                if (!valid)
                {
                    val = long.MaxValue;
                }
                this.decimalEscapes = this.decimalEscapes.Append(new decimalEscapeValue(start, this.pos(), val));
                return true;
            }
            return false;
        }

        // CharacterEscape ::=
        //
        //	| `c` ControlLetter
        //	| IdentityEscape
        //	| (Other sequences handled by `scanEscapeSequence`)
        //
        // IdentityEscape ::=
        //
        //	| '^' | '$' | '/' | '\' | '.' | '*' | '+' | '?' | '(' | ')' | '[' | ']' | '{' | '}' | '|'
        //	| [~AnyUnicodeMode] (any other non-identifier characters)
        // regexp.go character operands borrow Go string slices until range/rune
        // checks consume them; transformed escape outputs retain their own owner.
        private static (int Rune, int Size) decodeOperandRune(ByteStringValue text) =>
            text.IsBytes ? StringUtil.DecodeJSStringRune(text.ByteSpan)
                : StringUtil.DecodeJSStringRune(text.CharSpan);

        private ByteStringValue scanCharacterEscape(bool atomEscape)
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '\\')));
            var ch = this.charCode();
            switch (ch)
            {
                case -1:
                    {
                        this.error(Diagnostics.Undetermined_character_escape, (this.pos() - 1), 1);
                        return ByteStringValue.FromString("\\");
                    }
                case 'c':
                    {
                        this.incPos(1);
                        ch = this.charCode();
                        if (StringUtil.IsASCIILetter(ch))
                        {
                            this.incPos(1);
                            return GoString.FromRuneValue((ch & 0x1f));
                        }
                        if (this.anyUnicodeModeOrNonAnnexB)
                        {
                            this.error(Diagnostics.X_c_must_be_followed_by_an_ASCII_letter, (this.pos() - 2), 2);
                        }
                        else
                        {
                            if (atomEscape)
                            {
                                this.incPos(-1);
                                return ByteStringValue.FromString("\\");
                            }
                        }
                        return GoString.FromRuneValue(ch);
                    }
                case '^':
                case '$':
                case '/':
                case '\\':
                case '.':
                case '*':
                case '+':
                case '?':
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '|':
                    {
                        this.incPos(1);
                        return GoString.FromRuneValue(ch);
                    }
                default:
                    {
                        this.incPos(-1);
                        var flags = EscapeSequenceScanningFlags.RegularExpression;
                        if (this.annexB)
                        {
                            flags |= EscapeSequenceScanningFlags.AnnexB;
                        }
                        if (this.anyUnicodeMode)
                        {
                            flags |= EscapeSequenceScanningFlags.AnyUnicodeMode;
                        }
                        if (atomEscape)
                        {
                            flags |= EscapeSequenceScanningFlags.AtomEscape;
                        }
                        return this.scanner.scanEscapeSequence(flags);
                    }
            }
        }

        private void scanGroupName(bool isReference)
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '<')));
            this.scanner.tokenStart = this.pos();
            if (!this.scanner.scanIdentifier(0, identifierVariant.RegExpGroupName))
            {
                this.error(Diagnostics.Expected_a_capturing_group_name, this.pos(), 0);
            }
            else
            {
                if (isReference)
                {
                    this.groupNameReferences = this.groupNameReferences.Append(new groupNameReference(this.scanner.tokenStart, this.pos(), this.scanner.TokenValueTextValue()));
                }
                else
                {
                    if (this.namedCapturingGroupsContains(this.scanner.TokenValueTextValue()))
                    {
                        this.error(Diagnostics.Named_capturing_groups_with_the_same_name_must_be_mutually_exclusive_to_each_other, this.scanner.tokenStart, (this.pos() - this.scanner.tokenStart));
                    }
                    else
                    {
                        if ((((this.scanner.TokenValueTextValue().TryGetValue(this.groupSpecifiers, out var found) && found) && (this.scanner.languageVersion() >= ScriptTarget.ES2018)) && (this.scanner.languageVersion() < ScriptTarget.ES2025)))
                        {
                            this.error(Diagnostics.Duplicate_named_capturing_groups_are_only_available_when_targeting_0_or_later, this.scanner.tokenStart, (this.pos() - this.scanner.tokenStart), ToLower(ScriptTarget.ES2025.ToString()));
                        }
                        if ((this.namedCapturingGroups.Count > 0))
                        {
                            this.namedCapturingGroups[(this.namedCapturingGroups.Count - 1)][ByteStringKey.FromValue(this.scanner.TokenValueTextValue())] = true;
                        }
                        this.groupSpecifiers[ByteStringKey.FromValue(this.scanner.TokenValueTextValue())] = true;
                    }
                }
            }
        }

        private bool namedCapturingGroupsContains(ByteStringValue name)
        {
            foreach (var group in this.namedCapturingGroups)
            {
                if ((name.TryGetValue(group, out var found) && found))
                {
                    return true;
                }
            }
            return false;
        }

        private bool isClassContentExit(int ch)
        {
            return ((ch == ']') || (this.pos() >= this.end));
        }

        // ClassRanges ::= '^'? (ClassAtom ('-' ClassAtom)?)*
        private void scanClassRanges()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '[')));
            this.pendingLowSurrogate = 0;
            if ((this.charCode() == '^'))
            {
                this.incPos(1);
            }
            while ((this.pos() < this.end))
            {
                var ch = this.charCode();
                if (this.isClassContentExit(ch))
                {
                    return;
                }
                var minStart = this.pos();
                var minCharacter = this.scanClassAtom();
                if ((this.charCode() == '-'))
                {
                    this.incPos(1);
                    ch = this.charCode();
                    if (this.isClassContentExit(ch))
                    {
                        return;
                    }
                    if (((minCharacter.Length == 0) && this.anyUnicodeModeOrNonAnnexB))
                    {
                        this.error(Diagnostics.A_character_class_range_must_not_be_bounded_by_another_character_class, minStart, ((this.pos() - 1) - minStart));
                    }
                    var maxStart = this.pos();
                    var maxCharacter = this.scanClassAtom();
                    if (((maxCharacter.Length == 0) && this.anyUnicodeModeOrNonAnnexB))
                    {
                        this.error(Diagnostics.A_character_class_range_must_not_be_bounded_by_another_character_class, maxStart, (this.pos() - maxStart));
                        continue;
                    }
                    if ((minCharacter.Length == 0))
                    {
                        continue;
                    }
                    var (minCharacterValue, minSize) = decodeOperandRune(minCharacter);
                    var (maxCharacterValue, maxSize) = decodeOperandRune(maxCharacter);
                    if ((((minCharacter.Length == minSize) && (maxCharacter.Length == maxSize)) && (minCharacterValue > maxCharacterValue)))
                    {
                        this.error(Diagnostics.Range_out_of_order_in_character_class, minStart, (this.pos() - minStart));
                    }
                }
            }
        }

        // ClassSetExpression ::= '^'? (ClassUnion | ClassIntersection | ClassSubtraction)
        // ClassUnion ::= (ClassSetRange | ClassSetOperand)*
        // ClassIntersection ::= ClassSetOperand ('&&' ClassSetOperand)+
        // ClassSubtraction ::= ClassSetOperand ('--' ClassSetOperand)+
        // ClassSetRange ::= ClassSetCharacter '-' ClassSetCharacter
        private void scanClassSetExpression()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '[')));
            var isCharacterComplement = false;
            if ((this.charCode() == '^'))
            {
                this.incPos(1);
                isCharacterComplement = true;
            }
            var expressionMayContainStrings = false;
            var ch = this.charCode();
            if (this.isClassContentExit(ch))
            {
                return;
            }
            var start = this.pos();
            ByteStringValue operand = default;
            // Go regexp.go:607–619/698–710: these bounded slices only select ASCII operators.
            ReadOnlySpan<byte> twoChars = default;
            if (((this.pos() + 1) < this.end))
            {
                twoChars = this.text().AsSpan(this.pos(), 2);
            }
            switch (true)
            {
                case true when GoStrings.Equal(twoChars, "--"u8) || GoStrings.Equal(twoChars, "&&"u8):
                    {
                        this.error(Diagnostics.Expected_a_class_set_operand, this.pos(), 0);
                        this.mayContainStrings = false;
                        break;
                    }
                default:
                    {
                        operand = this.scanClassSetOperand();
                        break;
                    }
            }
            switch (this.charCode())
            {
                case '-':
                    {
                        if ((((this.pos() + 1) < this.end) && (this.charAt((this.pos() + 1)) == '-')))
                        {
                            if ((isCharacterComplement && this.mayContainStrings))
                            {
                                this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, start, (this.pos() - start));
                            }
                            expressionMayContainStrings = this.mayContainStrings;
                            this.scanClassSetSubExpression(classSetExpressionType.ClassSubtraction);
                            this.mayContainStrings = (!isCharacterComplement && expressionMayContainStrings);
                            return;
                        }
                        break;
                    }
                case '&':
                    {
                        if ((((this.pos() + 1) < this.end) && (this.charAt((this.pos() + 1)) == '&')))
                        {
                            this.scanClassSetSubExpression(classSetExpressionType.ClassIntersection);
                            if ((isCharacterComplement && this.mayContainStrings))
                            {
                                this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, start, (this.pos() - start));
                            }
                            expressionMayContainStrings = this.mayContainStrings;
                            this.mayContainStrings = (!isCharacterComplement && expressionMayContainStrings);
                            return;
                        }
                        break;
                    }
                default:
                    {
                        if ((isCharacterComplement && this.mayContainStrings))
                        {
                            this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, start, (this.pos() - start));
                        }
                        expressionMayContainStrings = this.mayContainStrings;
                        break;
                    }
            }
            while ((this.pos() < this.end))
            {
                ch = this.charCode();
                switch (ch)
                {
                    case '-':
                        {
                            this.incPos(1);
                            ch = this.charCode();
                            if (this.isClassContentExit(ch))
                            {
                                this.mayContainStrings = (!isCharacterComplement && expressionMayContainStrings);
                                return;
                            }
                            if ((ch == '-'))
                            {
                                this.incPos(1);
                                this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, (this.pos() - 2), 2);
                                start = (this.pos() - 2);
                                operand = ByteStringValue.FromView(this.text().Slice(start, this.pos()));
                                continue;
                            }
                            else
                            {
                                if ((operand.Length == 0))
                                {
                                    this.error(Diagnostics.A_character_class_range_must_not_be_bounded_by_another_character_class, start, ((this.pos() - 1) - start));
                                }
                                var secondStart = this.pos();
                                var secondOperand = this.scanClassSetOperand();
                                if ((isCharacterComplement && this.mayContainStrings))
                                {
                                    this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, secondStart, (this.pos() - secondStart));
                                }
                                expressionMayContainStrings = (expressionMayContainStrings || this.mayContainStrings);
                                if ((secondOperand.Length == 0))
                                {
                                    this.error(Diagnostics.A_character_class_range_must_not_be_bounded_by_another_character_class, secondStart, (this.pos() - secondStart));
                                }
                                else
                                {
                                    if ((operand.Length != 0))
                                    {
                                        var (minCharacterValue, minSize) = decodeOperandRune(operand);
                                        var (maxCharacterValue, maxSize) = decodeOperandRune(secondOperand);
                                        if ((((operand.Length == minSize) && (secondOperand.Length == maxSize)) && (minCharacterValue > maxCharacterValue)))
                                        {
                                            this.error(Diagnostics.Range_out_of_order_in_character_class, start, (this.pos() - start));
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case '&':
                        {
                            if ((((this.pos() + 1) < this.end) && (this.charAt((this.pos() + 1)) == '&')))
                            {
                                start = this.pos();
                                this.incPos(2);
                                this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, (this.pos() - 2), 2);
                                if ((this.charCode() == '&'))
                                {
                                    this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, this.pos(), 1, GoString.FromRune(ch));
                                    this.incPos(1);
                                }
                                operand = ByteStringValue.FromView(this.text().Slice(start, this.pos()));
                                continue;
                            }
                            break;
                        }
                }
                if (this.isClassContentExit(this.charCode()))
                {
                    break;
                }
                start = this.pos();
                twoChars = default;
                if (((this.pos() + 1) < this.end))
                {
                    twoChars = this.text().AsSpan(this.pos(), 2);
                }
                switch (true)
                {
                    case true when GoStrings.Equal(twoChars, "--"u8) || GoStrings.Equal(twoChars, "&&"u8):
                        {
                            this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, this.pos(), 2);
                            this.incPos(2);
                            operand = ByteStringValue.FromView(this.text().Slice(start, this.pos()));
                            break;
                        }
                    default:
                        {
                            operand = this.scanClassSetOperand();
                            if ((isCharacterComplement && this.mayContainStrings))
                            {
                                this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, start, (this.pos() - start));
                            }
                            expressionMayContainStrings = (expressionMayContainStrings || this.mayContainStrings);
                            break;
                        }
                }
            }
            this.mayContainStrings = (!isCharacterComplement && expressionMayContainStrings);
        }

        private void scanClassSetSubExpression(classSetExpressionType expressionType)
        {
            var expressionMayContainStrings = this.mayContainStrings;
            while ((this.pos() < this.end))
            {
                var ch = this.charCode();
                if (this.isClassContentExit(ch))
                {
                    break;
                }
                switch (ch)
                {
                    case '-':
                        {
                            this.incPos(1);
                            if ((this.charCode() == '-'))
                            {
                                this.incPos(1);
                                if ((expressionType != classSetExpressionType.ClassSubtraction))
                                {
                                    this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, (this.pos() - 2), 2);
                                }
                            }
                            else
                            {
                                this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, (this.pos() - 1), 1);
                            }
                            break;
                        }
                    case '&':
                        {
                            this.incPos(1);
                            if ((this.charCode() == '&'))
                            {
                                this.incPos(1);
                                if ((expressionType != classSetExpressionType.ClassIntersection))
                                {
                                    this.error(Diagnostics.Operators_must_not_be_mixed_within_a_character_class_Wrap_it_in_a_nested_class_instead, (this.pos() - 2), 2);
                                }
                                if ((this.charCode() == '&'))
                                {
                                    this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, this.pos(), 1, GoString.FromRune(ch));
                                    this.incPos(1);
                                }
                            }
                            else
                            {
                                this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, (this.pos() - 1), 1, GoString.FromRune(ch));
                            }
                            break;
                        }
                    default:
                        {
                            switch (expressionType)
                            {
                                case classSetExpressionType.ClassSubtraction:
                                    {
                                        this.error(Diagnostics.X_0_expected, this.pos(), 0, "--");
                                        break;
                                    }
                                case classSetExpressionType.ClassIntersection:
                                    {
                                        this.error(Diagnostics.X_0_expected, this.pos(), 0, "&&");
                                        break;
                                    }
                            }
                            break;
                        }
                }
                ch = this.charCode();
                if (this.isClassContentExit(ch))
                {
                    this.error(Diagnostics.Expected_a_class_set_operand, this.pos(), 0);
                    break;
                }
                this.scanClassSetOperand();
                if ((expressionType == classSetExpressionType.ClassIntersection))
                {
                    expressionMayContainStrings = (expressionMayContainStrings && this.mayContainStrings);
                }
            }
            this.mayContainStrings = expressionMayContainStrings;
        }

        // ClassSetOperand ::=
        //
        //	| '[' ClassSetExpression ']'
        //	| '\' CharacterClassEscape
        //	| '\q{' ClassStringDisjunctionContents '}'
        //	| ClassSetCharacter
        private ByteStringValue scanClassSetOperand()
        {
            this.mayContainStrings = false;
            switch (this.charCode())
            {
                case '[':
                    {
                        this.incPos(1);
                        this.scanClassSetExpression();
                        this.scanExpectedChar(']');
                        return ByteStringValue.FromString("");
                    }
                case '\\':
                    {
                        this.incPos(1);
                        if (this.scanCharacterClassEscape())
                        {
                            return ByteStringValue.FromString("");
                        }
                        else
                        {
                            if ((this.charCode() == 'q'))
                            {
                                this.incPos(1);
                                if ((this.charCode() == '{'))
                                {
                                    this.incPos(1);
                                    this.scanClassStringDisjunctionContents();
                                    this.scanExpectedChar('}');
                                    return ByteStringValue.FromString("");
                                }
                                else
                                {
                                    this.error(Diagnostics.X_q_must_be_followed_by_string_alternatives_enclosed_in_braces, (this.pos() - 2), 2);
                                    return ByteStringValue.FromString("q");
                                }
                            }
                        }
                        this.incPos(-1);
                        goto default;
                    }
                default:
                    {
                        return this.scanClassSetCharacter();
                    }
            }
        }

        // ClassStringDisjunctionContents ::= ClassSetCharacter* ('|' ClassSetCharacter*)*
        private void scanClassStringDisjunctionContents()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '{')));
            var characterCount = 0;
            while ((this.pos() < this.end))
            {
                var ch = this.charCode();
                switch (ch)
                {
                    case '}':
                        {
                            if ((characterCount != 1))
                            {
                                this.mayContainStrings = true;
                            }
                            return;
                        }
                    case '|':
                        {
                            if ((characterCount != 1))
                            {
                                this.mayContainStrings = true;
                            }
                            this.incPos(1);
                            characterCount = 0;
                            break;
                        }
                    default:
                        {
                            this.scanClassSetCharacter();
                            characterCount++;
                            break;
                        }
                }
            }
        }

        // ClassSetCharacter ::=
        //
        //	| SourceCharacter -- ClassSetSyntaxCharacter -- ClassSetReservedDoublePunctuator
        //	| '\' (CharacterEscape | ClassSetReservedPunctuator | 'b')
        private ByteStringValue scanClassSetCharacter()
        {
            var ch = this.charCode();
            if ((ch == '\\'))
            {
                this.incPos(1);
                var innerCh = this.charCode();
                switch (innerCh)
                {
                    case 'b':
                        {
                            this.incPos(1);
                            return ByteStringValue.FromString("\b");
                        }
                    case '&':
                    case '-':
                    case '!':
                    case '#':
                    case '%':
                    case ',':
                    case ':':
                    case ';':
                    case '<':
                    case '=':
                    case '>':
                    case '@':
                    case '`':
                    case '~':
                        {
                            this.incPos(1);
                            return GoString.FromRuneValue(innerCh);
                        }
                    default:
                        {
                            return this.scanCharacterEscape(false);
                        }
                }
            }
            else
            {
                if ((((this.pos() + 1) < this.end) && (ch == this.charAt((this.pos() + 1)))))
                {
                    switch (ch)
                    {
                        case '&':
                        case '!':
                        case '#':
                        case '%':
                        case '*':
                        case '+':
                        case ',':
                        case '.':
                        case ':':
                        case ';':
                        case '<':
                        case '=':
                        case '>':
                        case '?':
                        case '@':
                        case '`':
                        case '~':
                            {
                                this.error(Diagnostics.A_character_class_must_not_contain_a_reserved_double_punctuator_Did_you_mean_to_escape_it_with_backslash, this.pos(), 2);
                                this.incPos(2);
                                return ByteStringValue.FromView(this.text().Slice(this.pos() - 2, this.pos()));
                            }
                    }
                }
            }
            switch (ch)
            {
                case '/':
                case '(':
                case ')':
                case '[':
                case ']':
                case '{':
                case '}':
                case '-':
                case '|':
                    {
                        this.error(Diagnostics.Unexpected_0_Did_you_mean_to_escape_it_with_backslash, this.pos(), 1, GoString.FromRune(ch));
                        this.incPos(1);
                        return GoString.FromRuneValue(ch);
                    }
            }
            return this.scanSourceCharacter();
        }

        // ClassAtom ::=
        //
        //	| SourceCharacter but not one of '\' or ']'
        //	| '\' ClassEscape
        //
        // ClassEscape ::=
        //
        //	| 'b'
        //	| '-'
        //	| CharacterClassEscape
        //	| CharacterEscape
        private ByteStringValue scanClassAtom()
        {
            if ((this.charCode() == '\\'))
            {
                this.incPos(1);
                var ch = this.charCode();
                switch (ch)
                {
                    case 'b':
                        {
                            this.incPos(1);
                            return ByteStringValue.FromString("\b");
                        }
                    case '-':
                        {
                            this.incPos(1);
                            return GoString.FromRuneValue(ch);
                        }
                    default:
                        {
                            if (this.scanCharacterClassEscape())
                            {
                                return ByteStringValue.FromString("");
                            }
                            return this.scanCharacterEscape(false);
                        }
                }
            }
            else
            {
                return this.scanSourceCharacter();
            }
        }

        // CharacterClassEscape ::=
        //
        //	| 'd' | 'D' | 's' | 'S' | 'w' | 'W'
        //	| [+AnyUnicodeMode] ('P' | 'p') '{' UnicodePropertyValueExpression '}'
        private bool scanCharacterClassEscape()
        {
            Debug.Assert(((this.pos() > 0) && (this.text()[(this.pos() - 1)] == '\\')));
            var isCharacterComplement = false;
            var start = (this.pos() - 1);
            var ch = this.charCode();
            switch (ch)
            {
                case 'd':
                case 'D':
                case 's':
                case 'S':
                case 'w':
                case 'W':
                    {
                        this.incPos(1);
                        return true;
                    }
                case 'P':
                    {
                        isCharacterComplement = true;
                        goto case 'p';
                    }
                case 'p':
                    {
                        this.incPos(1);
                        if ((this.charCode() == '{'))
                        {
                            this.incPos(1);
                            var propertyNameOrValueStart = this.pos();
                            var propertyNameOrValue = this.scanWordCharacters();
                            if ((this.charCode() == '='))
                            {
                                var propertyName = (propertyNameOrValue.TryGetValue(nonBinaryUnicodeProperties, out var canonicalName) ? canonicalName : "");
                                if ((this.pos() == propertyNameOrValueStart))
                                {
                                    this.error(Diagnostics.Expected_a_Unicode_property_name, this.pos(), 0);
                                }
                                else
                                {
                                    if ((propertyName == ""))
                                    {
                                        this.error(Diagnostics.Unknown_Unicode_property_name, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart));
                                        var suggestion = this.getSpellingSuggestionForUnicodePropertyName(propertyNameOrValue.Materialize()!);
                                        if ((suggestion != ""))
                                        {
                                            this.error(Diagnostics.Did_you_mean_0, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart), suggestion);
                                        }
                                    }
                                }
                                this.incPos(1);
                                var propertyValueStart = this.pos();
                                var propertyValue = this.scanWordCharacters();
                                if ((this.pos() == propertyValueStart))
                                {
                                    this.error(Diagnostics.Expected_a_Unicode_property_value, this.pos(), 0);
                                }
                                else
                                {
                                    if ((propertyName != ""))
                                    {
                                        var values = valuesOfNonBinaryUnicodeProperties.GetValueOrDefault(ByteStringKey.FromString(propertyName));
                                        if (((values != null) && !propertyValue.ContainsIn(values)))
                                        {
                                            this.error(Diagnostics.Unknown_Unicode_property_value, propertyValueStart, (this.pos() - propertyValueStart));
                                            var suggestion = this.getSpellingSuggestionForUnicodePropertyValue(propertyName, propertyValue.Materialize()!);
                                            if ((suggestion != ""))
                                            {
                                                this.error(Diagnostics.Did_you_mean_0, propertyValueStart, (this.pos() - propertyValueStart), suggestion);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((this.pos() == propertyNameOrValueStart))
                                {
                                    this.error(Diagnostics.Expected_a_Unicode_property_name_or_value, this.pos(), 0);
                                }
                                else
                                {
                                    if (propertyNameOrValue.ContainsIn(binaryUnicodePropertiesOfStrings))
                                    {
                                        if (!this.unicodeSetsMode)
                                        {
                                            this.error(Diagnostics.Any_Unicode_property_that_would_possibly_match_more_than_a_single_character_is_only_available_when_the_Unicode_Sets_v_flag_is_set, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart));
                                        }
                                        else
                                        {
                                            if (isCharacterComplement)
                                            {
                                                this.error(Diagnostics.Anything_that_would_possibly_match_more_than_a_single_character_is_invalid_inside_a_negated_character_class, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart));
                                            }
                                            else
                                            {
                                                this.mayContainStrings = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if ((!propertyNameOrValue.ContainsIn(valuesOfNonBinaryUnicodeProperties[ByteStringKey.FromString("General_Category")]) && !propertyNameOrValue.ContainsIn(binaryUnicodeProperties)))
                                        {
                                            this.error(Diagnostics.Unknown_Unicode_property_name_or_value, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart));
                                            var suggestion = this.getSpellingSuggestionForUnicodePropertyNameOrValue(propertyNameOrValue.Materialize()!);
                                            if ((suggestion != ""))
                                            {
                                                this.error(Diagnostics.Did_you_mean_0, propertyNameOrValueStart, (this.pos() - propertyNameOrValueStart), suggestion);
                                            }
                                        }
                                    }
                                }
                            }
                            this.scanExpectedChar('}');
                            if (!this.anyUnicodeMode)
                            {
                                this.error(Diagnostics.Unicode_property_value_expressions_are_only_available_when_the_Unicode_u_flag_or_the_Unicode_Sets_v_flag_is_set, start, (this.pos() - start));
                            }
                        }
                        else
                        {
                            if (this.anyUnicodeModeOrNonAnnexB)
                            {
                                this.error(Diagnostics.X_0_must_be_followed_by_a_Unicode_property_value_expression_enclosed_in_braces, (this.pos() - 2), 2, GoString.FromRune(ch));
                            }
                            else
                            {
                                this.incPos(-1);
                                return false;
                            }
                        }
                        return true;
                    }
            }
            return false;
        }

        private string getSpellingSuggestionForUnicodePropertyName(string name)
        {
            return Core.GetSpellingSuggestionForStrings(name, nonBinaryUnicodeProperties.Keys.Select(key => key.ToString()));
        }

        private string getSpellingSuggestionForUnicodePropertyValue(string propertyName, string value)
        {
            var values = valuesOfNonBinaryUnicodeProperties.GetValueOrDefault(ByteStringKey.FromString(propertyName));
            if ((values == null))
            {
                return "";
            }
            return Core.GetSpellingSuggestionForStrings(value, values.Select(key => key.ToString()));
        }

        private string getSpellingSuggestionForUnicodePropertyNameOrValue(string name)
        {
            return Core.GetSpellingSuggestionForStrings(name, valuesOfNonBinaryUnicodeProperties[ByteStringKey.FromString("General_Category")].Concat(binaryUnicodeProperties).Concat(binaryUnicodePropertiesOfStrings).Select(key => key.ToString()));
        }

        private ByteStringValue scanWordCharacters()
        {
            var start = this.pos();
            while ((this.pos() < this.end))
            {
                var ch = this.charCode();
                if (!isWordCharacter(ch))
                {
                    break;
                }
                this.incPos(1);
            }
            return ByteStringValue.FromView(this.text().Slice(start, this.pos()));
        }

        private ByteStringValue scanSourceCharacter()
        {
            if ((this.pos() >= this.end))
            {
                return ByteStringValue.FromString("");
            }
            if (!this.anyUnicodeMode)
            {
                if ((this.pendingLowSurrogate != 0))
                {
                    var (_, lowSize) = Utf8.DecodeRuneInString(this.text().AsSpan(this.pos()));
                    this.incPos(lowSize);
                    var low = this.pendingLowSurrogate;
                    this.pendingLowSurrogate = 0;
                    return StringUtil.EncodeJSStringRuneValue(low);
                }
                var (ch, size) = Utf8.DecodeRuneInString(this.text().AsSpan(this.pos()));
                if (((ch == Utf8.RuneError) || (size == 0)))
                {
                    this.incPos(1);
                    return GoString.FromRuneValue(this.text()[(this.pos() - 1)]);
                }
                if ((ch >= 0x10000 && ch <= 0x10FFFF))
                {
                    var (high, low) = StringUtil.CodePointToSurrogatePair(ch);
                    this.pendingLowSurrogate = low;
                    return StringUtil.EncodeJSStringRuneValue(high);
                }
                var start = this.pos();
                this.incPos(size);
                return ByteStringValue.FromView(this.text().Slice(start, this.pos()));
            }
            var (unicodeCh, unicodeSize) = Utf8.DecodeRuneInString(this.text().AsSpan(this.pos()));
            if ((unicodeSize == 0))
            {
                return ByteStringValue.FromString("");
            }
            if ((unicodeCh == Utf8.RuneError))
            {
                this.incPos(unicodeSize);
                return ByteStringValue.FromString("");
            }
            var unicodeStart = this.pos();
            this.incPos(unicodeSize);
            return ByteStringValue.FromView(this.text().Slice(unicodeStart, this.pos()));
        }

        private void scanExpectedChar(int ch)
        {
            if ((this.charCode() == ch))
            {
                this.incPos(1);
            }
            else
            {
                this.error(Diagnostics.X_0_expected, this.pos(), 0, GoString.FromRune(ch));
            }
        }

        private void scanDigits()
        {
            var start = this.pos();
            while (((this.pos() < this.end) && StringUtil.IsDigit(this.charCode())))
            {
                this.incPos(1);
            }
            this.scanner.setTokenValueView(this.text().Slice(start, this.pos()));
        }

        public void run()
        {
            this.anyUnicodeModeOrNonAnnexB = (this.anyUnicodeMode || !this.annexB);
            this.scanDisjunction(false);
            foreach (var reference in this.groupNameReferences)
            {
                if (!(reference.name.TryGetValue(this.groupSpecifiers, out var found) && found))
                {
                    this.error(Diagnostics.There_is_no_capturing_group_named_0_in_this_regular_expression, reference.pos, (reference.end - reference.pos), reference.name.Materialize()!);
                    if ((this.groupSpecifiers.Count > 0))
                    {
                        var suggestion = Core.GetSpellingSuggestionForStrings(reference.name.Materialize()!, this.groupSpecifiers.Keys.Select(key => key.ToString()));
                        if ((suggestion != ""))
                        {
                            this.error(Diagnostics.Did_you_mean_0, reference.pos, (reference.end - reference.pos), suggestion);
                        }
                    }
                }
            }
            foreach (var escape in this.decimalEscapes)
            {
                if ((escape.value > this.numberOfCapturingGroups))
                {
                    if ((this.numberOfCapturingGroups > 0))
                    {
                        this.error(Diagnostics.This_backreference_refers_to_a_group_that_does_not_exist_There_are_only_0_capturing_groups_in_this_regular_expression, escape.pos, (escape.end - escape.pos), this.numberOfCapturingGroups);
                    }
                    else
                    {
                        this.error(Diagnostics.This_backreference_refers_to_a_group_that_does_not_exist_There_are_no_capturing_groups_in_this_regular_expression, escape.pos, (escape.end - escape.pos));
                    }
                }
            }
        }

    }
}
