using System;
using Musician_Module.Controls.Instrument;
using Musician_Module.Notation.Parsers;
using Musician_Module.Notation.Persistance;
using Musician_Module.Player.Algorithms;
using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Blish_HUD;

namespace Musician_Module.Player
{
    public enum KeyboardType
    {
        Emulated,
        Preview,
        Practice
    }
    internal static class MusicPlayerFactory
    {
        private static KeyboardPractice PracticeKeyboard = new KeyboardPractice();
        private static Keyboard EmulatedKeyboard = new Keyboard();
        private static Dictionary<string, Instrument> InstrumentRepository = new Dictionary<string, Instrument>()
        {
            { "harp", new Harp(new HarpPreview()) },
            { "flute", new Flute(new FlutePreview()) },
            { "lute", new Lute(new LutePreview()) },
            { "horn", new Horn(new HornPreview()) },
            { "bass", new Bass(new BassPreview()) },
            { "bell", new Bell(new BellPreview()) },
            { "bell2", new Bell2(new Bell2Preview()) },
        };
        internal static MusicPlayer Create(RawMusicSheet rawMusicSheet, KeyboardType type)
        {
            return MusicBoxNotationMusicPlayerFactory(rawMusicSheet, type);
        }
        private static MusicPlayer MusicBoxNotationMusicPlayerFactory(RawMusicSheet rawMusicSheet, KeyboardType type)
        {
            var musicSheet = new MusicSheetParser(new ChordParser(new NoteParser(), rawMusicSheet.Instrument)).Parse(
                rawMusicSheet.Melody,
                int.Parse(rawMusicSheet.Tempo),
                int.Parse(rawMusicSheet.Meter.Split('/')[0]),
                int.Parse(rawMusicSheet.Meter.Split('/')[1]));


            var algorithm = rawMusicSheet.Algorithm == "favor notes"
                ? new FavorNotesAlgorithm() : (IPlayAlgorithm)new FavorChordsAlgorithm();

            Instrument instrument = InstrumentRepository[rawMusicSheet.Instrument];
            switch (type)
            {
                case KeyboardType.Preview:
                    MusicianModule.ModuleInstance.Conveyor.Visible = false;
                    instrument.Keyboard = instrument.PreviewKeyboard;
                    break;
                case KeyboardType.Practice:
                    MusicianModule.ModuleInstance.Conveyor.Visible = true;
                    instrument.Keyboard = PracticeKeyboard;
                    break;
                case KeyboardType.Emulated:
                    MusicianModule.ModuleInstance.Conveyor.Visible = false;
                    instrument.Keyboard = EmulatedKeyboard;
                    break;
                default:
                    throw new NotSupportedException();
            }
            return new MusicPlayer(
                musicSheet,
                instrument,
                algorithm);
        }
    }
}