﻿using System.Collections.Generic;
using System.Linq;
using Souvenir;
using UnityEngine;

using Rnd = UnityEngine.Random;

public partial class SouvenirModule
{
    private IEnumerable<object> ProcessGamepad(KMBombModule module)
    {
        var comp = GetComponent(module, "GamepadModule");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.05f);
        _modulesSolved.IncSafe(_Gamepad);

        var x = GetIntField(comp, "x").Get(min: 1, max: 99);
        var y = GetIntField(comp, "y").Get(min: 1, max: 99);
        var display = GetField<GameObject>(comp, "Input", isPublic: true).Get().GetComponent<TextMesh>();
        var digits1 = GetField<GameObject>(comp, "Digits1", isPublic: true).Get().GetComponent<TextMesh>();
        var digits2 = GetField<GameObject>(comp, "Digits2", isPublic: true).Get().GetComponent<TextMesh>();

        if (display == null || digits1 == null || digits2 == null)
            throw new AbandonModuleException("One of the three displays does not have a TextMesh ({0}, {1}, {2}).",
                display == null ? "null" : "not null", digits1 == null ? "null" : "not null", digits2 == null ? "null" : "not null");

        addQuestions(module, makeQuestion(Question.GamepadNumbers, _Gamepad, correctAnswers: new[] { string.Format("{0:00}:{1:00}", x, y) },
            preferredWrongAnswers: Enumerable.Range(0, int.MaxValue).Select(i => string.Format("{0:00}:{1:00}", Rnd.Range(1, 99), Rnd.Range(1, 99))).Distinct().Take(6).ToArray()));
        digits1.GetComponent<TextMesh>().text = "--";
        digits2.GetComponent<TextMesh>().text = "--";
    }

    private IEnumerable<object> ProcessGrayCipher(KMBombModule module)
    {
        return processColoredCiphers(module, "grayCipher", Question.GrayCipherAnswer, _GrayCipher);
    }

    private IEnumerable<object> ProcessGreatVoid(KMBombModule module)
    {
        var comp = GetComponent(module, "TheGreatVoid");
        var fldSolved = GetField<bool>(comp, "Solved");
        var fldDigits = GetArrayField<int>(comp, "Displays");
        var fldColors = GetArrayField<int>(comp, "ColorNums");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_GreatVoid);

        var colorNames = new[] { "Red", "Green", "Blue", "Magenta", "Yellow", "Cyan", "White" };

        var questions = new List<QandA>();
        for (int i = 0; i < 6; i++)
        {
            questions.Add(makeQuestion(Question.GreatVoidDigit, _GreatVoid, new[] { ordinal(i + 1) }, new[] { fldDigits.Get()[i].ToString() }));
            questions.Add(makeQuestion(Question.GreatVoidColor, _GreatVoid, new[] { ordinal(i + 1) }, new[] { colorNames[fldColors.Get()[i]] }));
        }
        addQuestions(module, questions);
    }

    private IEnumerable<object> ProcessGreenArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "GreenArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNumDisplay = GetField<GameObject>(comp, "numDisplay", isPublic: true);
        var fldStreak = GetIntField(comp, "streak");
        var fldAnimating = GetField<bool>(comp, "isanimating");

        string numbers = null;
        bool activated = false;
        while (!fldSolved.Get())
        {
            int streak = fldStreak.Get();
            bool animating = fldAnimating.Get();
            if (streak == 6 && !animating && !activated)
            {
                var numDisplay = fldNumDisplay.Get();
                numbers = numDisplay.GetComponent<TextMesh>().text;
                if (numbers == null)
                    throw new AbandonModuleException("numDisplay TextMesh text was null.");
                activated = true;
            }
            if (streak == 0)
                activated = false;
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_GreenArrows);

        int number;
        if (!int.TryParse(numbers, out number))
            throw new AbandonModuleException("The screen is not an integer: “{0}”.", number);
        if (number < 0 || number > 99)
            throw new AbandonModuleException("The number on the screen is out of range: number = {1}, expected 0-99", number);

        addQuestions(module, makeQuestion(Question.GreenArrowsLastScreen, _GreenArrows, correctAnswers: new[] { number.ToString("00") }));
    }

    private IEnumerable<object> ProcessGreenCipher(KMBombModule module)
    {
        return processColoredCiphers(module, "greenCipher", Question.GreenCipherAnswer, _GreenCipher);
    }

    private IEnumerable<object> ProcessGridLock(KMBombModule module)
    {
        var comp = GetComponent(module, "GridlockModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var colors = GetAnswers(Question.GridLockStartingColor);

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var solution = GetIntField(comp, "_solution").Get(min: 0, max: 15);
        var pages = GetArrayField<int[]>(comp, "_pages").Get(minLength: 5, maxLength: 10, validator: p => p.Length != 16 ? "expected length 16" : p.Any(q => q < 0 || (q & 15) > 12 || (q & (15 << 4)) > (4 << 4)) ? "unexpected value" : null);
        var start = pages[0].IndexOf(i => (i & 15) == 4);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_GridLock);
        addQuestions(module,
            makeQuestion(Question.GridLockStartingLocation, _GridLock, preferredWrongAnswers: Tiles4x4Sprites, correctAnswers: new[] { Tiles4x4Sprites[start] }),
            makeQuestion(Question.GridLockEndingLocation, _GridLock, preferredWrongAnswers: Tiles4x4Sprites, correctAnswers: new[] { Tiles4x4Sprites[solution] }),
            makeQuestion(Question.GridLockStartingColor, _GridLock, correctAnswers: new[] { colors[(pages[0][start] >> 4) - 1] }));
    }

    private IEnumerable<object> ProcessGroceryStore(KMBombModule module)
    {
        var comp = GetComponent(module, "GroceryStoreBehav");
        var solved = false;
        var display = GetField<TextMesh>(comp, "displayTxt", isPublic: true);
        var items = GetField<Dictionary<string, float>>(comp, "itemPrices").Get().Keys.ToArray();

        var finalAnswer = display.Get().text;
        module.OnPass += delegate { solved = true; return false; };

        var hadStrike = false;
        module.OnStrike += delegate { hadStrike = true; return false; };

        while (!solved)
        {
            if (hadStrike)
            {
                finalAnswer = display.Get().text;
                hadStrike = false;
            }
            yield return null;
        }

        _modulesSolved.IncSafe(_GroceryStore);
        addQuestions(module, makeQuestion(Question.GroceryStoreFirstItem, _GroceryStore, null, new[] { finalAnswer }, items));
    }

    private IEnumerable<object> ProcessGryphons(KMBombModule module)
    {
        var comp = GetComponent(module, "Gryphons");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Gryphons);

        var age = GetIntField(comp, "age").Get(23, 34);
        var name = GetField<string>(comp, "theirName").Get();

        addQuestions(module,
            makeQuestion(Question.GryphonsName, _Gryphons, correctAnswers: new[] { name }),
            makeQuestion(Question.GryphonsAge, _Gryphons, correctAnswers: new[] { age.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(23, 34).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessGuessWho(KMBombModule module)
    {
        var comp = GetComponent(module, "GuessWhoScript");
        var names = GetField<string[]>(comp, "Names", isPublic: true).Get();

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_GuessWho);

        var correctAnswer = names[GetField<int>(comp, "TheCombination").Get()];
        addQuestions(module, makeQuestion(Question.GuessWhoPerson, _GuessWho, null, new[] { correctAnswer }, names));
    }
}