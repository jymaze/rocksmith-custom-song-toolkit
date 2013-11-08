﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.DLCPackage.AggregateGraph;
using RocksmithToolkitLib.Sng;

namespace RocksmithToolkitGUI.DLCPackageCreator
{
    public partial class ArrangementForm : Form
    {
        private Arrangement arrangement;
        private DLCPackageCreator parentControl = null;

        public ArrangementForm(IEnumerable<string> toneNames, DLCPackageCreator control)
            : this(new Arrangement
            {
                SongFile = new SongFile { File = "" },
                SongXml = new SongXML { File = "" },
                ArrangementType = ArrangementType.Guitar,
                RelativeDifficulty = 1,
                ScrollSpeed = 20
            }, toneNames, control)
        {
        }

        public ArrangementForm(Arrangement arrangement, IEnumerable<string> toneNames, DLCPackageCreator control)
        {
            InitializeComponent();
            
            foreach (var val in Enum.GetValues(typeof(InstrumentTuning))) {
                tuningComboBox.Items.Add(val);
            }
            foreach (var val in Enum.GetValues(typeof(ArrangementType)))
            {
                arrangementTypeCombo.Items.Add(val);
            }
            arrangementTypeCombo.SelectedValueChanged += (sender, e) => {
                // Selecting defaults
                ArrangementType selectedType = ((ArrangementType)((ComboBox)sender).SelectedItem);
                
                switch (selectedType) {
                    case ArrangementType.Bass:
                        arrangementNameCombo.Items.Clear();
                        arrangementNameCombo.Items.Add(ArrangementName.Bass);
                        arrangementNameCombo.SelectedItem = ArrangementName.Bass;
                        break;
                    case ArrangementType.Vocal:
                        arrangementNameCombo.Items.Clear();
                        arrangementNameCombo.Items.Add(ArrangementName.Vocals);
                        arrangementNameCombo.SelectedItem = ArrangementName.Vocals;
                        break;
                    default:
                        arrangementNameCombo.Items.Clear();
                        arrangementNameCombo.Items.Add(ArrangementName.Combo);
                        arrangementNameCombo.Items.Add(ArrangementName.Lead);
                        arrangementNameCombo.Items.Add(ArrangementName.Rhythm);
                        arrangementNameCombo.SelectedItem = arrangement.Name;
                        break;
                }
                // Disabling options that are not meant for Arrangement Types
                arrangementNameCombo.Enabled = selectedType == ArrangementType.Guitar;
                gbTone.Enabled = selectedType != ArrangementType.Vocal;
                gbScrollSpeed.Enabled = selectedType != ArrangementType.Guitar;
                Picked.Visible = selectedType == ArrangementType.Bass;
                toneBaseCombo.Enabled = selectedType != ArrangementType.Vocal;
                tuningComboBox.Enabled = selectedType != ArrangementType.Vocal;
                Picked.Checked = selectedType == ArrangementType.Bass ? false : true;
                
                MasterId.Enabled = selectedType != ArrangementType.Vocal;
                PersistentId.Enabled = selectedType != ArrangementType.Vocal;
            };
            foreach (var tone in toneNames)
            {
                toneBaseCombo.Items.Add(tone);
            }
            scrollSpeedTrackBar.Scroll += (sender, e) =>
            {
                scrollSpeedDisplay.Text = String.Format("Scroll speed: {0:#.0}", Math.Truncate((decimal)scrollSpeedTrackBar.Value) / 10);
            };
            Arrangement = arrangement;
            parentControl = control;
        }

        public Arrangement Arrangement
        {
            get
            {
                return arrangement;
            }
            private set
            {
                arrangement = value;
                
                //Arrangement details
                arrangementNameCombo.SelectedItem = arrangement.Name;
                arrangementTypeCombo.SelectedItem = arrangement.ArrangementType;
                
                InstrumentTuning tuning = InstrumentTuning.Standard;
                Enum.TryParse<InstrumentTuning>(arrangement.Tuning, true, out tuning);
                tuningComboBox.SelectedItem = tuning;

                toneBaseCombo.SelectedItem = arrangement.ToneName;
                if (toneBaseCombo.SelectedItem == null && toneBaseCombo.Items.Count > 0)
                {
                    toneBaseCombo.SelectedItem = toneBaseCombo.Items[0];
                }

                Picked.Checked = arrangement.PluckedType == PluckedType.Picked;
                RelativeDifficulty.Text = arrangement.RelativeDifficulty.ToString();

                int scrollSpeed = Math.Min(scrollSpeedTrackBar.Maximum, Math.Max(scrollSpeedTrackBar.Minimum, arrangement.ScrollSpeed));
                scrollSpeedTrackBar.Value = scrollSpeed;
                scrollSpeedDisplay.Text = String.Format("Scroll speed: {0:#.0}", Math.Truncate((decimal)scrollSpeed) / 10);

                //Song xml file
                XmlFilePath.Text = arrangement.SongXml.File;
                
                PersistentId.Text = arrangement.Id.ToString().Replace("-", "").ToUpper();
                MasterId.Text = arrangement.MasterId.ToString();
            }
        }

        private void songXmlBrowseButton_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Xml Files|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                    XmlFilePath.Text = ofd.FileName;
                else
                    return;
            }

            try {
                //Read XML chart info (generated by EoF) and try to pre-input some fields
                var doc = XDocument.Load(XmlFilePath.Text);

                bool isVocal = doc.XPathSelectElement("/vocals") != null;

                if (isVocal) {
                    arrangementTypeCombo.SelectedItem = ArrangementType.Vocal;
                } else {
                    Form parentForm = (this.Parent as Form);

                    if (String.IsNullOrEmpty(parentControl.SongTitle)) parentControl.SongTitle = doc.XPathSelectElement("/song/title") != null ? doc.XPathSelectElement("/song/title").Value : String.Empty;
                    if (String.IsNullOrEmpty(parentControl.SongTitleSort)) parentControl.SongTitleSort = parentControl.SongTitle;
                    if (String.IsNullOrEmpty(parentControl.DLCName)) parentControl.DLCName = parentControl.SongTitleSort;
                    if (String.IsNullOrEmpty(parentControl.AverageTempo)) parentControl.AverageTempo = doc.XPathSelectElement("/song/averageTempo") != null ? doc.XPathSelectElement("/song/averageTempo").Value : String.Empty;
                    if (String.IsNullOrEmpty(parentControl.Artist)) parentControl.Artist = doc.XPathSelectElement("/song/artistName") != null ? doc.XPathSelectElement("/song/artistName").Value : String.Empty;
                    if (String.IsNullOrEmpty(parentControl.ArtistSort)) parentControl.ArtistSort = parentControl.Artist;
                    if (String.IsNullOrEmpty(parentControl.Album)) parentControl.Album = doc.XPathSelectElement("/song/albumName") != null ? doc.XPathSelectElement("/song/albumName").Value : String.Empty;
                    if (String.IsNullOrEmpty(parentControl.AlbumYear)) parentControl.AlbumYear = doc.XPathSelectElement("/song/albumYear") != null ? doc.XPathSelectElement("/song/albumYear").Value : String.Empty;

                    string arr = doc.XPathSelectElement("/song/arrangement").Value;
                    if (arr.ToLower().IndexOf("guitar") > -1 || arr.ToLower().IndexOf("lead") > -1 || arr.ToLower().IndexOf("rhythm") > -1 || arr.ToLower().IndexOf("combo") > -1)
                    {
                        arrangementTypeCombo.SelectedItem = ArrangementType.Guitar;
                        if (arr.ToLower().IndexOf("guitar 22") > -1 || arr.ToLower().IndexOf("rhythm") > -1)
                            arrangementNameCombo.SelectedItem = ArrangementName.Rhythm;
                    }
                    if (arr.ToLower().IndexOf("bass") > -1)
                        arrangementTypeCombo.SelectedItem = ArrangementType.Bass;

                    XElement arrangementProperties = doc.XPathSelectElement("/song/arrangementProperties");
                    if (arrangementProperties != null && arrangementProperties.Attributes() != null && arrangementProperties.Attributes().Count() > 25) {
                        Picked.Checked = Convert.ToBoolean(Convert.ToInt16(arrangementProperties.Attribute("bassPick").Value));

                        bool standardTuning = Convert.ToBoolean(Convert.ToInt16(arrangementProperties.Attribute("standardTuning").Value));
                        if (!standardTuning) {
                            XElement tuning = doc.XPathSelectElement("/song/tuning");
                            int[] strings = {
                                            Convert.ToInt32(tuning.Attribute("string0").Value),
                                            Convert.ToInt32(tuning.Attribute("string1").Value),
                                            Convert.ToInt32(tuning.Attribute("string2").Value),
                                            Convert.ToInt32(tuning.Attribute("string3").Value),
                                            Convert.ToInt32(tuning.Attribute("string4").Value),
                                            Convert.ToInt32(tuning.Attribute("string5").Value)
                                        };
                            tuningComboBox.SelectedItem = InstrumentTuningExtensions.GetTuningByOffsets(strings);
                        }
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("Unable to get information from the arrangement XML. \r\nYour version of the EoF is up to date? \r\n" + ex.Message, "DLC Package Creator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private int readInt(string val)
        {
            int v;
            if (int.TryParse(val, out v) == false)
                return -1;
            return v;
        }

        private void addArrangementButton_Click(object sender, EventArgs e)
        {
            //Validations
            var xmlfilepath = XmlFilePath.Text;
            if (!File.Exists(xmlfilepath))
            {
                XmlFilePath.Focus();
                return;
            }

            if (arrangement.RelativeDifficulty == -1) {
                RelativeDifficulty.Focus();
                return;
            }

            Guid guid;
            if (Guid.TryParse(PersistentId.Text, out guid) == false) {
            	PersistentId.Focus();
            } else {
            	arrangement.Id = guid;
            }

            int masterId;
            if (int.TryParse(MasterId.Text, out masterId) == false) {
			    MasterId.Focus();        	
            } else {
            	arrangement.MasterId = masterId;
            }
            
            //Arrangment information
            arrangement.Name = (ArrangementName)arrangementNameCombo.SelectedItem;
            arrangement.ArrangementType = (ArrangementType)arrangementTypeCombo.SelectedItem;
            arrangement.Tuning = tuningComboBox.SelectedItem.ToString();
            arrangement.PluckedType = Picked.Checked ? PluckedType.Picked : PluckedType.NotPicked;
            arrangement.RelativeDifficulty = readInt(RelativeDifficulty.Text);
            arrangement.ScrollSpeed = scrollSpeedTrackBar.Value;

            // New tone definition for RS2014
            arrangement.ToneName = toneBaseCombo.SelectedItem.ToString(); //TODO: SETUP TONE RS2014
            
            //Song xml file
            arrangement.SongXml.File = xmlfilepath;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
