﻿using System;
using System.Collections.Generic;
using System.Linq;
using Souvenir;
using UnityEngine;
using Rnd = UnityEngine.Random;

public partial class SouvenirModule
{
    private IEnumerable<object> ProcessNecronomicon(KMBombModule module)
    {
        var comp = GetComponent(module, "necronomiconScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Necronomicon);

        int[] chapters = GetArrayField<int>(comp, "selectedChapters").Get(expectedLength: 7);
        string[] chaptersString = chapters.Select(x => x.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "first" }, new[] { chaptersString[0] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "second" }, new[] { chaptersString[1] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "third" }, new[] { chaptersString[2] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fourth" }, new[] { chaptersString[3] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fifth" }, new[] { chaptersString[4] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "sixth" }, new[] { chaptersString[5] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "seventh" }, new[] { chaptersString[6] }, chaptersString));
    }

    private IEnumerable<object> ProcessNegativity(KMBombModule module)
    {
        var comp = GetComponent(module, "NegativityScript");
        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Negativity);

        var convertedNums = GetArrayField<int>(comp, "NumberingConverted").Get();
        var expectedTotal = GetField<int>(comp, "Totale").Get();
        var submittedTernary = GetField<string>(comp, "Tables").Get(str => str.Any(ch => !"+-".Contains(ch)) ? "At least 1 character from the submitted ternary is not familar. (Accepted: '+','-')" : null);

        // Generate possible incorrect answers for this module
        var incorrectValues = new HashSet<int>();
        while (incorrectValues.Count < 5)
        {
            var sumPossible = 0;
            for (var i = 0; i < convertedNums.Length; i++)
            {
                var aValue = convertedNums[i];
                if (Rnd.Range(0, 2) != 0)
                    sumPossible += aValue;
                else
                    sumPossible -= aValue;
            }
            if (sumPossible != expectedTotal)
                incorrectValues.Add(sumPossible);
        }

        var incorrectSubmittedTernary = new HashSet<string>();
        while (incorrectSubmittedTernary.Count < 5)
        {
            var onePossible = "";
            var wantedLength = Rnd.Range(Mathf.Max(2, submittedTernary.Length - 1), Mathf.Min(11, Mathf.Max(submittedTernary.Length + 1, 5)));
            for (var x = 0; x < wantedLength; x++)
                onePossible += "+-".PickRandom();
            if (onePossible != submittedTernary)
                incorrectSubmittedTernary.Add(onePossible);
        }

        addQuestions(module,
            makeQuestion(Question.NegativitySubmittedValue, _Negativity, null, new[] { expectedTotal.ToString() }, incorrectValues.Select(a => a.ToString()).ToArray()),
            makeQuestion(Question.NegativitySubmittedTernary, _Negativity, null, new[] { string.IsNullOrEmpty(submittedTernary) ? "(empty)" : submittedTernary }, incorrectSubmittedTernary.ToArray()));
    }

    private IEnumerable<object> ProcessNeutralization(KMBombModule module)
    {
        var comp = GetComponent(module, "neutralization");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var acidType = GetIntField(comp, "acidType").Get(min: 0, max: 3);
        var acidVol = GetIntField(comp, "acidVol").Get(av => av < 5 || av > 20 || av % 5 != 0 ? "unexpected acid volume" : null);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Neutralization);

        var colorText = GetField<GameObject>(comp, "colorText", isPublic: true).Get(nullAllowed: true);
        if (colorText != null)
            colorText.SetActive(false);

        addQuestions(module,
            makeQuestion(Question.NeutralizationColor, _Neutralization, correctAnswers: new[] { new[] { "Yellow", "Green", "Red", "Blue" }[acidType] }),
            makeQuestion(Question.NeutralizationVolume, _Neutralization, correctAnswers: new[] { acidVol.ToString() }));
    }

    private IEnumerable<object> ProcessNandMs(KMBombModule module)
    {
        var comp = GetComponent(module, "NandMs");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NandMs);

        var words = GetArrayField<string>(comp, "otherWords").Get();
        var index = GetIntField(comp, "otherwordindex").Get(min: 0, max: words.Length - 1);
        addQuestion(module, Question.NandMsAnswer, correctAnswers: new[] { words[index] });
    }

    private IEnumerable<object> ProcessNavinums(KMBombModule module)
    {
        var comp = GetComponent(module, "navinumsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStage = GetIntField(comp, "stage");
        var fldDirections = GetListField<int>(comp, "directions");
        var lookUp = GetArrayField<int[]>(comp, "lookUp").Get(expectedLength: 9, validator: ar => ar.Length != 8 ? "expected length 8" : null);
        var directionsSorted = GetListField<int>(comp, "directionsSorted").Get(expectedLength: 4);
        var centerDigit = GetIntField(comp, "center").Get(min: 1, max: 9);

        var curStage = -1;
        var answers = new int[8];
        while (true)
        {
            yield return null;
            var newStage = fldStage.Get();
            if (newStage != curStage)
            {
                if (newStage == 8)
                    break;
                var newDirections = fldDirections.Get();
                if (newDirections.Count != 4)
                    throw new AbandonModuleException("‘directions’ has unexpected length {1} (expected 4).", newDirections.Count);

                answers[newStage] = newDirections.IndexOf(directionsSorted[lookUp[centerDigit - 1][newStage] - 1]);
                if (answers[newStage] == -1)
                    throw new AbandonModuleException("‘directions’ ({0}) does not contain the value from ‘directionsSorted’ ({1}).",
                        newDirections.JoinString(", "), directionsSorted[lookUp[centerDigit - 1][newStage] - 1]);
                curStage = newStage;
            }
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Navinums);

        var directionNames = new[] { "up", "left", "right", "down" };

        var qs = new List<QandA>();
        for (var stage = 0; stage < 8; stage++)
            qs.Add(makeQuestion(Question.NavinumsDirectionalButtons, _Navinums, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { directionNames[answers[stage]] }));
        qs.Add(makeQuestion(Question.NavinumsMiddleDigit, _Navinums, correctAnswers: new[] { centerDigit.ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNotKeypad(KMBombModule module)
    {
        var comp = GetComponent(module, "NotKeypad");
        var connectorComponent = GetComponent(module, "NotVanillaModulesLib.NotKeypadConnector");
        var propSolved = GetProperty<bool>(comp, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotKeypad);

        var strings = GetAnswers(Question.NotKeypadColor);
        var colours = GetField<Array>(comp, "sequenceColours").Get(ar => ar.Cast<int>().Any(v => v <= 0 || v > strings.Length) ? "out of range" : null);
        var buttons = GetArrayField<int>(comp, "sequenceButtons").Get(expectedLength: colours.Length);
        var symbols = GetField<Array>(connectorComponent, "symbols").Get(ar => ar.Cast<int>().Any(v => v < 0 || v > KeypadSprites.Length) ? "out of range" : null);
        var sprites = symbols.Cast<int>().Select(i => KeypadSprites[i]).ToArray();

        var qs = new List<QandA>();
        for (var stage = 0; stage < colours.Length; stage++)
        {
            qs.Add(makeQuestion(Question.NotKeypadColor, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { strings[(int) colours.GetValue(stage) - 1] }));
            qs.Add(makeQuestion(Question.NotKeypadSymbol, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { KeypadSprites[(int) symbols.GetValue(buttons[stage])] }, sprites));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNotMaze(KMBombModule module)
    {
        var component = GetComponent(module, "NotMaze");
        var propSolved = GetProperty<bool>(component, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMaze);

        addQuestion(module, Question.NotMazeStartingDistance, correctAnswers: new[] { GetIntField(component, "distance").Get().ToString() });
    }

    private IEnumerable<object> ProcessNotMorseCode(KMBombModule module)
    {
        var component = GetComponent(module, "NotMorseCode");
        var propSolved = GetProperty<bool>(component, "Solved", true);

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMorseCode);

        var words = GetArrayField<string>(component, "words").Get();
        var channels = GetArrayField<int>(component, "correctChannels").Get();
        var columns = GetStaticField<string[][]>(component.GetType(), "defaultColumns").Get();

        addQuestions(module, Enumerable.Range(0, 5).Select(stage => makeQuestion(
            question: Question.NotMorseCodeWord,
            moduleKey: _NotMorseCode,
            formatArgs: new[] { ordinal(stage + 1) },
            correctAnswers: new[] { words[channels[stage]] },
            preferredWrongAnswers: words.Concat(Enumerable.Range(0, 50).Select(_ => columns.PickRandom().PickRandom())).Except(new[] { words[channels[stage]] }).Distinct().Take(8).ToArray())));
    }

    private IEnumerable<object> ProcessNotSimaze(KMBombModule module)
    {
        var comp = GetComponent(module, "NotSimaze");
        var propSolved = GetProperty<bool>(comp, "Solved", isPublic: true);
        var fldMazeIndex = GetIntField(comp, "mazeIndex");

        var colours = GetAnswers(Question.NotSimazeMaze);
        var startPositionArray = new[] { string.Format("({0}, {1})", colours[GetIntField(comp, "x").Get()], colours[GetIntField(comp, "y").Get()]) };

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotSimaze);

        var goalPositionArray = new[] { string.Format("({0}, {1})", colours[GetIntField(comp, "goalX").Get()], colours[GetIntField(comp, "goalY").Get()]) };

        addQuestions(module,
            makeQuestion(Question.NotSimazeMaze, _NotSimaze, correctAnswers: new[] { colours[fldMazeIndex.Get()] }),
            makeQuestion(Question.NotSimazeStart, _NotSimaze, correctAnswers: startPositionArray, preferredWrongAnswers: goalPositionArray),
            makeQuestion(Question.NotSimazeGoal, _NotSimaze, correctAnswers: goalPositionArray, preferredWrongAnswers: startPositionArray));
    }

    private IEnumerable<object> ProcessNotTheButton(KMBombModule module)
    {
        var comp = GetComponent(module, "NotButton");
        var propSolved = GetProperty<bool>(comp, "Solved", isPublic: true);
        var propLightColour = GetProperty<object>(comp, "LightColour", isPublic: true); // actual type is an enum

        var lightColor = 0;
        while (!propSolved.Get())
        {
            lightColor = (int) propLightColour.Get();   // casting boxed enum value to int
            yield return null;  // Don’t wait for .1 seconds so we don’t miss it
        }
        _modulesSolved.IncSafe(_NotTheButton);

        if (lightColor != 0)
        {
            var strings = GetAnswers(Question.NotTheButtonLightColor);
            if (lightColor <= 0 || lightColor > strings.Length)
                throw new AbandonModuleException("‘LightColour’ is out of range ({0}).", lightColor);
            addQuestion(module, Question.NotTheButtonLightColor, correctAnswers: new[] { strings[lightColor - 1] });
        }
        else
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Not the Button because the strip didn’t light up (or I missed the light color).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
    }

    private IEnumerable<object> ProcessNotWhosOnFirst(KMBombModule module)
    {
        var comp = GetComponent(module, "NotWhosOnFirst");
        var propSolved = GetProperty<bool>(comp, "Solved", true);
        var fldPositions = GetArrayField<int>(comp, "rememberedPositions");
        var fldLabels = GetArrayField<string>(comp, "rememberedLabels");
        var fldSum = GetIntField(comp, "stage2Sum");

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotWhosOnFirst);

        var positions = GetAnswers(Question.NotWhosOnFirstPressedPosition);
        var sumCorrectAnswers = new[] { fldSum.Get().ToString() };

        var qs = new List<QandA>();
        for (var i = 0; i < 4; i++)
        {
            qs.Add(makeQuestion(Question.NotWhosOnFirstPressedPosition, _NotWhosOnFirst, new[] { ordinal(i + 1) }, new[] { positions[fldPositions.Get()[i]] }));
            qs.Add(makeQuestion(Question.NotWhosOnFirstPressedLabel, _NotWhosOnFirst, new[] { ordinal(i + 1) }, new[] { fldLabels.Get()[i] }));
        }
        for (var i = 4; i < 6; i++)
        {
            qs.Add(makeQuestion(Question.NotWhosOnFirstReferencePosition, _NotWhosOnFirst, new[] { ordinal(i - 1) }, new[] { positions[fldPositions.Get()[i]] }));
            qs.Add(makeQuestion(Question.NotWhosOnFirstReferenceLabel, _NotWhosOnFirst, new[] { ordinal(i - 1) }, new[] { fldLabels.Get()[i] }));
        }
        qs.Add(makeQuestion(Question.NotWhosOnFirstSum, _NotWhosOnFirst, correctAnswers: sumCorrectAnswers));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNotXRay(KMBombModule module)
    {
        var comp = GetComponent(module, "NotXRayModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotXRay);

        var table = GetIntField(comp, "_table").Get(0, 7);
        var directions = GetField<Array>(comp, "_directions").Get(validator: arr => arr.Length != 4 ? "expected length 4" : null);
        var allColors = GetAnswers(Question.NotXRayScannerColor);
        var scannerColor = GetField<object>(comp, "_scannerColor").Get(v => v == null ? "did not expected null" : !allColors.Contains(v.ToString()) ? "expected " + allColors.JoinString(", ") : null);

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.NotXRayTable, _NotXRay, correctAnswers: new[] { (table + 1).ToString() }));
        qs.Add(makeQuestion(Question.NotXRayScannerColor, _NotXRay, correctAnswers: new[] { scannerColor.ToString() }));
        for (var i = 0; i < 4; i++)
        {
            qs.Add(makeQuestion(Question.NotXRayDirections, _NotXRay, formatArgs: new[] { (i + 1).ToString() }, correctAnswers: new[] { directions.GetValue(i).ToString() }));
            qs.Add(makeQuestion(Question.NotXRayButtons, _NotXRay, formatArgs: new[] { directions.GetValue(i).ToString().ToLowerInvariant() }, correctAnswers: new[] { (i + 1).ToString() }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessNumberedButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "NumberedButtonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var expectedButtons = GetListField<string>(comp, "ExpectedButtons").Get(list => list.Count == 0 ? "list is empty" : null).ToArray();

        var hadStrike = false;
        module.OnStrike += delegate { hadStrike = true; return false; };

        while (!fldSolved.Get())
        {
            yield return null;
            if (hadStrike)
            {
                yield return null;
                expectedButtons = GetListField<string>(comp, "ExpectedButtons").Get(list => list.Count == 0 ? "list is empty" : null).ToArray();
            }
        }
        _modulesSolved.IncSafe(_NumberedButtons);
        addQuestion(module, Question.NumberedButtonsButtons, correctAnswers: expectedButtons);
    }

    private IEnumerable<object> ProcessNumbers(KMBombModule module)
    {
        var comp = GetComponent(module, "WAnumbersScript");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Numbers);

        var numberValue1 = GetField<int>(comp, "numberValue1").Get();
        var numberValue2 = GetField<int>(comp, "numberValue2").Get();
        var answer = numberValue1.ToString() + numberValue2.ToString();
        addQuestions(module, makeQuestion(Question.NumbersTwoDigit, _Numbers, null, new[] { answer }));
    }
}