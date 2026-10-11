using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
public class Alarm {
 public string Id=Guid.NewGuid().ToString(), Name="Alarm";
 public int Hour=8, Minute=0, Second=0, Days=0; public bool Enabled=true;
 public string SoundPath="alarm.wav", SoundName="Classic";
 public bool IsInterval=false;
 public int IntervalMinutes=5, TotalCount=12, FiredCount=0;
 public DateTime IntervalDue=DateTime.MinValue;
 [XmlIgnore] public DateTime Due;
 public DateTime Next(DateTime now) {
  if(IsInterval) return now.AddMinutes(IntervalMinutes);
  for(int i=0;i<8;i++){DateTime d=now.Date.AddDays(i).AddHours(Hour).AddMinutes(Minute).AddSeconds(Second);if(d>now&&(Days==0||(Days&(1<<(int)d.DayOfWeek))!=0))return d;}
  return DateTime.MaxValue;
 }
 public void Initialize(DateTime now,bool restart) {
  if(!IsInterval){Due=Next(now);return;}
  if(IntervalMinutes<1||IntervalMinutes>1440||TotalCount<1||TotalCount>10000||FiredCount<0)throw new InvalidDataException("Invalid interval settings.");
  if(restart){FiredCount=0;IntervalDue=now.AddMinutes(IntervalMinutes);}
  else if(FiredCount>=TotalCount){Enabled=false;}
  else if(IntervalDue==DateTime.MinValue||IntervalDue<=now){IntervalDue=now.AddMinutes(IntervalMinutes);}
  Due=IntervalDue;
 }
 public void Fire(DateTime now) {
  if(IsInterval){
   FiredCount++;
   if(FiredCount>=TotalCount){Enabled=false;Due=DateTime.MaxValue;IntervalDue=DateTime.MinValue;}
   else {DateTime next=Due.AddMinutes(IntervalMinutes);if(next<=now)next=now.AddMinutes(IntervalMinutes);IntervalDue=next;Due=next;}
  } else if(Days==0)Enabled=false;else Due=Next(now);
 }
}
class Wake : IDisposable {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateWaitableTimer(IntPtr a,bool b,string c);
 [DllImport("kernel32.dll")] static extern bool SetWaitableTimer(IntPtr h,ref long d,int p,IntPtr c,IntPtr a,bool resume);
 [DllImport("kernel32.dll")] static extern bool CancelWaitableTimer(IntPtr h);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
 IntPtr h=CreateWaitableTimer(IntPtr.Zero,true,null);
 public bool Arm(DateTime d){if(h==IntPtr.Zero)return false;if(d==DateTime.MaxValue){CancelWaitableTimer(h);return true;}long t=d.ToUniversalTime().ToFileTimeUtc();return SetWaitableTimer(h,ref t,0,IntPtr.Zero,IntPtr.Zero,true);}
 public void Dispose(){if(h!=IntPtr.Zero){CancelWaitableTimer(h);CloseHandle(h);h=IntPtr.Zero;}}
}

class ToneOption {
 public string Name,Path;
 public ToneOption(string name,string path){Name=name;Path=path;}
 public override string ToString(){return L.Sound(Name);}
}
class TonePlayer:IDisposable {
 [DllImport("winmm.dll",CharSet=CharSet.Unicode,EntryPoint="mciSendStringW",ExactSpelling=true)] static extern uint Send(string command,StringBuilder result,int size,IntPtr callback);
 [DllImport("winmm.dll",CharSet=CharSet.Unicode,EntryPoint="mciGetErrorStringW",ExactSpelling=true)] static extern bool ErrorText(uint code,StringBuilder text,int size);
 string alias="tone"+Guid.NewGuid().ToString("N");bool opened=false,isMp3=false;System.Windows.Forms.Timer loop;
 public static string Resolve(string value){if(string.IsNullOrEmpty(value))value="alarm.wav";return Path.IsPathRooted(value)?value:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,value);}
 void Command(string text){uint code=Send(text,null,0,IntPtr.Zero);if(code!=0){var error=new StringBuilder(512);ErrorText(code,error,error.Capacity);throw new InvalidOperationException("Audio playback failed: "+error.ToString()+" ("+code+")");}}
 public void Load(string path){
  Dispose();path=Resolve(path);if(!File.Exists(path))throw new FileNotFoundException("Sound file not found.",path);
  string ext=Path.GetExtension(path).ToLowerInvariant();if(ext!=".wav"&&ext!=".mp3")throw new InvalidDataException("Select a WAV or MP3 file.");isMp3=ext==".mp3";
  Command("open \""+path+"\" type "+(isMp3?"mpegvideo":"waveaudio")+" alias "+alias);opened=true;
  if(!isMp3){loop=new System.Windows.Forms.Timer();loop.Interval=200;loop.Tick+=delegate{if(!opened)return;try{var mode=new StringBuilder(64);uint code=Send("status "+alias+" mode",mode,mode.Capacity,IntPtr.Zero);if(code==0&&mode.ToString()=="stopped")Command("play "+alias+" from 0");}catch{Dispose();}};}
 }
 public void Play(){if(!opened)throw new InvalidOperationException("No sound loaded.");Command("play "+alias+" from 0"+(isMp3?" repeat":""));if(loop!=null)loop.Start();}
 public void Dispose(){if(loop!=null){loop.Dispose();loop=null;}if(opened){Send("stop "+alias,null,0,IntPtr.Zero);Send("close "+alias,null,0,IntPtr.Zero);opened=false;}}
}

class Editor:Form {
 public Alarm Result;
 TextBox name=new TextBox(); DateTimePicker time=new DateTimePicker(); CheckedListBox days=new CheckedListBox();CheckBox enabled=new CheckBox();ComboBox mode=new ComboBox();NumericUpDown minutes=new NumericUpDown(),count=new NumericUpDown();Button current=new Button();Label hint=new Label();ComboBox tones=new ComboBox();TonePlayer preview=new TonePlayer();
 public Editor(Alarm a){
  Text=a==null?L.T("Add alarm"):L.T("Edit alarm");ClientSize=new Size(420,660);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
  Controls.Add(new Label{Text=L.T("Name"),Left=20,Top=18,AutoSize=true});name.SetBounds(20,42,380,28);name.Text=a==null?L.T("Alarm"):a.Name;Controls.Add(name);
  mode.DropDownStyle=ComboBoxStyle.DropDownList;mode.Items.AddRange(new object[]{L.T("Scheduled"),L.T("Interval (start now)")});mode.SetBounds(20,85,260,30);Controls.Add(mode);
  enabled.Text=L.T("Enabled");enabled.Checked=a==null||a.Enabled;enabled.SetBounds(300,85,95,28);Controls.Add(enabled);
  Controls.Add(new Label{Text=L.T("Alarm time"),Left=20,Top=130,AutoSize=true});time.Format=DateTimePickerFormat.Custom;time.CustomFormat="HH:mm:ss";time.ShowUpDown=true;time.SetBounds(20,155,170,28);time.Value=a==null?DateTime.Now:DateTime.Today.AddHours(a.Hour).AddMinutes(a.Minute).AddSeconds(a.Second);Controls.Add(time);
  current.Text=L.T("Use current time");current.SetBounds(205,153,195,32);current.Click+=delegate{time.Value=DateTime.Now;};Controls.Add(current);
  Controls.Add(new Label{Text=L.T("Repeat days (none = once)"),Left=20,Top=200,AutoSize=true});days.SetBounds(20,225,380,145);days.CheckOnClick=true;days.Items.AddRange(new object[]{L.T("Sunday"),L.T("Monday"),L.T("Tuesday"),L.T("Wednesday"),L.T("Thursday"),L.T("Friday"),L.T("Saturday")});if(a!=null)for(int i=0;i<7;i++)days.SetItemChecked(i,(a.Days&(1<<i))!=0);Controls.Add(days);
  Controls.Add(new Label{Text=L.T("Every"),Left=20,Top=393,AutoSize=true});minutes.SetBounds(65,388,95,28);minutes.Minimum=1;minutes.Maximum=1440;minutes.Value=a==null?5:Math.Max(1,Math.Min(1440,a.IntervalMinutes));Controls.Add(minutes);
  Controls.Add(new Label{Text=L.T("min, total"),Left=168,Top=393,AutoSize=true});count.SetBounds(247,388,95,28);count.Minimum=1;count.Maximum=10000;count.Value=a==null?12:Math.Max(1,Math.Min(10000,a.TotalCount));Controls.Add(count);Controls.Add(new Label{Text=L.T("times"),Left=353,Top=393,AutoSize=true});
  hint.SetBounds(20,430,380,48);Controls.Add(hint);mode.SelectedIndexChanged+=delegate{UpdateMode();};mode.SelectedIndex=a!=null&&a.IsInterval?1:0;
  Controls.Add(new Label{Text=L.T("Alarm sound"),Left=20,Top=488,AutoSize=true});
  tones.DropDownStyle=ComboBoxStyle.DropDownList;tones.SetBounds(20,512,380,28);Controls.Add(tones);
  tones.Items.Add(new ToneOption("Classic","alarm.wav"));tones.Items.Add(new ToneOption("Soft bell","sounds/bell.wav"));tones.Items.Add(new ToneOption("Electronic pulse","sounds/pulse.wav"));tones.Items.Add(new ToneOption("Morning melody","sounds/melody.wav"));tones.Items.Add(new ToneOption("Urgent","sounds/urgent.wav"));
  int selected=0;if(a!=null){bool found=false;for(int i=0;i<tones.Items.Count;i++)if(((ToneOption)tones.Items[i]).Path==a.SoundPath){selected=i;found=true;break;}if(!found){selected=tones.Items.Add(new ToneOption(string.IsNullOrEmpty(a.SoundName)?L.T("Custom sound"):a.SoundName,a.SoundPath));}}
  tones.SelectedIndex=selected;tones.SelectedIndexChanged+=delegate{preview.Dispose();};
  Button import=new Button{Text=L.T("Import WAV / MP3"),Left=20,Top=553,Width=160,Height=32};import.Click+=delegate{ImportTone();};Controls.Add(import);
  Button listen=new Button{Text=L.T("Preview"),Left=190,Top=553,Width=95,Height=32};listen.Click+=delegate{try{preview.Load(((ToneOption)tones.SelectedItem).Path);preview.Play();}catch(Exception e){preview.Dispose();MessageBox.Show(this,L.Message(e.Message),L.T("Sound preview"));}};Controls.Add(listen);
  Button quiet=new Button{Text=L.T("Stop preview"),Left=295,Top=553,Width=105,Height=32};quiet.Click+=delegate{preview.Dispose();};Controls.Add(quiet);FormClosing+=delegate{preview.Dispose();};
  Button save=new Button{Text=L.T("Save"),Left=205,Top=610,Width=90,Height=32};save.Click+=delegate{
   int mask=0;for(int i=0;i<7;i++)if(days.GetItemChecked(i))mask|=1<<i;
   Result=new Alarm{Id=a==null?Guid.NewGuid().ToString():a.Id,Name=string.IsNullOrWhiteSpace(name.Text)?L.T("Alarm"):name.Text.Trim(),Hour=time.Value.Hour,Minute=time.Value.Minute,Second=time.Value.Second,Days=mask,Enabled=enabled.Checked,IsInterval=mode.SelectedIndex==1,IntervalMinutes=(int)minutes.Value,TotalCount=(int)count.Value,SoundPath=((ToneOption)tones.SelectedItem).Path,SoundName=((ToneOption)tones.SelectedItem).Name};
   DialogResult=DialogResult.OK;Close();};Controls.Add(save);AcceptButton=save;
  Button cancel=new Button{Text=L.T("Cancel"),Left=310,Top=610,Width=90,Height=32,DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;Theme.Style(this);AutoScroll=true;
 }
 void ImportTone(){
  using(OpenFileDialog dialog=new OpenFileDialog{Title=L.T("Choose alarm sound"),Filter=L.T("Audio (*.wav;*.mp3)|*.wav;*.mp3|WAV files|*.wav|MP3 files|*.mp3"),CheckFileExists=true,Multiselect=false}){
   if(dialog.ShowDialog(this)!=DialogResult.OK)return;
   try{
    using(TonePlayer check=new TonePlayer())check.Load(dialog.FileName);
    string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IndependentAlarm","Ringtones");Directory.CreateDirectory(directory);
    string imported=Path.Combine(directory,Guid.NewGuid().ToString("N")+Path.GetExtension(dialog.FileName).ToLowerInvariant());File.Copy(dialog.FileName,imported);
    tones.SelectedIndex=tones.Items.Add(new ToneOption("Custom: "+Path.GetFileName(dialog.FileName),imported));
   }catch(Exception e){MessageBox.Show(this,L.T("Cannot import sound: ")+e.Message,L.T("Import sound"));}
  }
 }
 protected override void Dispose(bool disposing){if(disposing)preview.Dispose();base.Dispose(disposing);}
 void UpdateMode(){bool interval=mode.SelectedIndex==1;time.Enabled=!interval;current.Enabled=!interval;days.Enabled=!interval;minutes.Enabled=interval;count.Enabled=interval;hint.Text=interval?L.T("First ring after one interval.\nSaving or re-enabling restarts the count."):L.T("Current time is filled automatically.\nPast times move to the next matching day.");}
}

public class UsageEntry {
 public string Id=Guid.NewGuid().ToString(),AlarmId,Name,AlarmType,Kind;
 public DateTime Start,End;
 public bool IsOpen;
 public int RingCount;
}

static class LegacyText { public static string Convert(string value){switch(value){case "\u95f9\u949f":return "Alarm";case "\u7ecf\u5178\u63d0\u793a":return "Classic";case "\u8f7b\u67d4\u949f\u58f0":return "Soft bell";case "\u7535\u5b50\u8109\u51b2":return "Electronic pulse";case "\u6668\u95f4\u65cb\u5f8b":return "Morning melody";case "\u6025\u4fc3\u63d0\u9192":return "Urgent";case "\u5b9a\u65f6\u95f9\u949f":return "Scheduled";case "\u95f4\u9694\u95f9\u949f":return "Interval";case "\u8ba1\u65f6":return "Usage";case "\u54cd\u94c3":return "Ringing";}return value;} }
class UsageHistory {
 public List<UsageEntry> Entries=new List<UsageEntry>();
 Dictionary<string,UsageEntry> usage=new Dictionary<string,UsageEntry>(),ring=new Dictionary<string,UsageEntry>();
 string file;public string Error="";bool readable=true;
 public UsageHistory(string path){file=path;try{if(File.Exists(file))using(var f=File.OpenRead(file))Entries=(List<UsageEntry>)new XmlSerializer(typeof(List<UsageEntry>)).Deserialize(f);foreach(var e in Entries){e.IsOpen=false;e.Kind=LegacyText.Convert(e.Kind);e.AlarmType=LegacyText.Convert(e.AlarmType);e.Name=LegacyText.Convert(e.Name);}}catch(Exception e){readable=false;Error="History load failed; original preserved: "+e.Message;}}
 public static double Seconds(UsageEntry e,DateTime from,DateTime to,DateTime now){DateTime end=e.IsOpen?now:e.End;DateTime a=e.Start>from?e.Start:from,b=end<to?end:to;return Math.Max(0,(b-a).TotalSeconds);}
 public static string Duration(double seconds){long n=(long)Math.Max(0,seconds);if(n>=3600)return (n/3600)+L.T("h ")+(n%3600/60)+L.T("m ");if(n>=60)return (n/60)+L.T("m ")+(n%60)+L.T("s");return n+L.T("s");}
 UsageEntry New(Alarm a,string kind,DateTime now){return new UsageEntry{AlarmId=a.Id,Name=a.Name,AlarmType=a.IsInterval?"Interval":"Scheduled",Kind=kind,Start=now,End=now,IsOpen=true};}
 public void StartUsage(Alarm a,DateTime now){if(!readable||!a.Enabled||usage.ContainsKey(a.Id))return;var e=New(a,"Usage",now);Entries.Add(e);usage[a.Id]=e;}
 public void StopUsage(string id,DateTime now){if(!usage.ContainsKey(id))return;var e=usage[id];e.End=now<e.Start?e.Start:now;e.IsOpen=false;usage.Remove(id);}
 public void Trigger(Alarm a){if(usage.ContainsKey(a.Id))usage[a.Id].RingCount++;}
 public void StartRing(Alarm a,DateTime now){if(!readable||ring.ContainsKey(a.Id))return;var e=New(a,"Ringing",now);Entries.Add(e);ring[a.Id]=e;}
 public void StopRing(string id,DateTime now){if(!ring.ContainsKey(id))return;var e=ring[id];e.End=now<e.Start?e.Start:now;e.IsOpen=false;ring.Remove(id);}
 public void CloseAll(DateTime now){foreach(var id in usage.Keys.ToArray())StopUsage(id,now);foreach(var id in ring.Keys.ToArray())StopRing(id,now);Save(now);}
 public void Save(DateTime now){if(!readable)return;foreach(var e in usage.Values.Concat(ring.Values))e.End=now<e.Start?e.Start:now;try{Directory.CreateDirectory(Path.GetDirectoryName(file));using(var f=File.Create(file+".tmp"))new XmlSerializer(typeof(List<UsageEntry>)).Serialize(f,Entries);if(File.Exists(file))File.Replace(file+".tmp",file,file+".bak");else File.Move(file+".tmp",file);Error="";}catch(Exception e){Error="History save failed: "+e.Message;}}
}
static class Theme {
 public static Color Blue=Color.FromArgb(52,104,240),Bg=Color.FromArgb(247,248,250),Text=Color.FromArgb(31,42,61),Muted=Color.FromArgb(108,119,139),Line=Color.FromArgb(226,232,242);
 public static Icon AppIcon(){try{return new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AppIcon-v1.ico"));}catch{return SystemIcons.Information;}}
 public static Label Label(string text,int x,int y,int width,int height,float size,bool bold){return new Label{Text=text,Left=x,Top=y,Width=width,Height=height,ForeColor=Text,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),BackColor=Color.Transparent};}
 public static Button Button(string text,Action action,bool primary){Button b=new Button{Text=L.T(text),Width=100,Height=36,FlatStyle=FlatStyle.Flat,BackColor=primary?Blue:Color.White,ForeColor=primary?Color.White:Text,Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=primary?Blue:Line;b.Click+=delegate{action();};return b;}
 public static void Style(Form f){f.BackColor=Bg;f.ForeColor=Text;f.Font=new Font("Microsoft YaHei UI",10);f.Icon=AppIcon();foreach(Control c in f.Controls){if(c is Button){var b=(Button)c;b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.White;b.ForeColor=Text;b.FlatAppearance.BorderColor=Line;if(b.Text==L.T("Save")||b.Text==L.T("Stop")){b.BackColor=Blue;b.ForeColor=Color.White;b.FlatAppearance.BorderColor=Blue;}}if(c is CheckedListBox)c.BackColor=Color.White;}}
}
class Card:Panel {
 public Card(){DoubleBuffered=true;BackColor=Theme.Bg;Padding=new Padding(16);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var p=new System.Drawing.Drawing2D.GraphicsPath()){int r=20,w=Width-1,h=Height-1;p.AddArc(0,0,r,r,180,90);p.AddArc(w-r,0,r,r,270,90);p.AddArc(w-r,h-r,r,r,0,90);p.AddArc(0,h-r,r,r,90,90);p.CloseFigure();using(var b=new SolidBrush(Color.White))e.Graphics.FillPath(b,p);using(var pen=new Pen(Theme.Line))e.Graphics.DrawPath(pen,p);}}
}
class UsageChart:Panel {
 public double[] ClockValues=new double[0],IntervalValues=new double[0];public DateTime Month;
 public UsageChart(){DoubleBuffered=true;BackColor=Color.White;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;int count=ClockValues.Length;if(count==0)return;double max=1;for(int i=0;i<count;i++)max=Math.Max(max,(ClockValues[i]+IntervalValues[i])/3600);max=Math.Ceiling(max);float left=55,top=25,bottom=Height-35,width=Math.Max(1,Width-left-20),height=Math.Max(1,bottom-top);using(var muted=new SolidBrush(Theme.Muted))using(var blue=new SolidBrush(Theme.Blue))using(var light=new SolidBrush(Color.FromArgb(161,189,255)))using(var line=new Pen(Theme.Line)){
 for(int row=0;row<=4;row++){float y=bottom-height*row/4;g.DrawLine(line,left,y,Width-15,y);g.DrawString((max*row/4).ToString("0.#")+L.T("h"),Font,muted,2,y-8);}
 float step=width/count,bw=Math.Max(2,step*.58f);for(int i=0;i<count;i++){float x=left+i*step+(step-bw)/2,h1=(float)(ClockValues[i]/3600/max*height),h2=(float)(IntervalValues[i]/3600/max*height);if(h1>0)g.FillRectangle(blue,x,bottom-h1,bw,h1);if(h2>0)g.FillRectangle(light,x,bottom-h1-h2,bw,h2);if(i==0||i==count-1||((i+1)%5==0))g.DrawString((i+1).ToString(),Font,muted,x-2,bottom+7);}}
 }
}

class App:Form {
 List<Alarm> alarms=new List<Alarm>();Dictionary<string,DateTime> snoozes=new Dictionary<string,DateTime>();Dictionary<string,Form> rings=new Dictionary<string,Form>();Dictionary<string,TonePlayer> players=new Dictionary<string,TonePlayer>();Dictionary<string,Label> nextLabels=new Dictionary<string,Label>();
 string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IndependentAlarm"),file;
 UsageHistory history;Panel content=new Panel();Panel pageBody;FlowLayoutPanel cards;Label clock,status;Button[] nav=new Button[3];NotifyIcon tray=new NotifyIcon();System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();Wake wake=new Wake();bool exit=false,finished=false;int page=0,ticks=0;
 DateTimePicker picker;ComboBox typeFilter,recordFilter,languageCombo;Panel statsBody;Label brandLabel,sideNote,languageLabel;bool changingLanguage=false;
 public App(){
  Text=L.T("AlarmApp");ClientSize=new Size(1080,700);MinimumSize=new Size(920,610);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Theme.Bg;ForeColor=Theme.Text;Icon=Theme.AppIcon();file=Path.Combine(directory,"alarms.xml");
  Panel sidebar=new Panel{Dock=DockStyle.Left,Width=195,BackColor=Color.White};Controls.Add(sidebar);PictureBox logo=new PictureBox{Left=20,Top=22,Width=42,Height=42,SizeMode=PictureBoxSizeMode.Zoom};try{logo.Image=Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AppIcon.png"));}catch{}sidebar.Controls.Add(logo);brandLabel=Theme.Label(L.T("AlarmApp"),72,28,110,35,14,true);sidebar.Controls.Add(brandLabel);
  string[] names={L.T("My alarms"),L.T("Summary"),L.T("History")};for(int i=0;i<3;i++){int index=i;nav[i]=Theme.Button(names[i],()=>ShowPage(index),false);nav[i].SetBounds(16,110+i*58,163,46);nav[i].TextAlign=ContentAlignment.MiddleLeft;nav[i].Padding=new Padding(12,0,0,0);nav[i].FlatAppearance.BorderSize=0;sidebar.Controls.Add(nav[i]);}sideNote=Theme.Label(L.T("Close to tray.\nAlarms keep running."),20,590,160,55,9,false);sideNote.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;sideNote.ForeColor=Theme.Muted;sidebar.Controls.Add(sideNote);
  languageLabel=Theme.Label(L.T("Language"),20,475,155,24,10,false);languageLabel.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;sidebar.Controls.Add(languageLabel);
  languageCombo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Left=20,Top=507,Width=155,Anchor=AnchorStyles.Bottom|AnchorStyles.Left};languageCombo.Items.AddRange(new object[]{"English","\u7b80\u4f53\u4e2d\u6587"});languageCombo.SelectedIndex=L.Language=="zh-CN"?1:0;sidebar.Controls.Add(languageCombo);
  languageCombo.SelectedIndexChanged+=delegate{if(changingLanguage)return;string previous=L.Language;try{L.Choose(languageCombo.SelectedIndex==1?"zh-CN":"en",L.PreferencePath);}catch{changingLanguage=true;languageCombo.SelectedIndex=previous=="zh-CN"?1:0;changingLanguage=false;MessageBox.Show(this,L.T("Could not save language preference."),L.T("Language settings"));return;}ApplyLanguage();};

  content.Dock=DockStyle.Fill;content.Padding=new Padding(26,22,26,18);Controls.Add(content);content.BringToFront();
  status=Theme.Label("",0,0,600,38,9,false);status.ForeColor=Theme.Muted;status.Dock=DockStyle.Bottom;content.Controls.Add(status);
  tray.Icon=Icon;tray.Text=L.T("AlarmApp");tray.Visible=true;ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add(L.T("Open AlarmApp"),null,delegate{Restore();});menu.Items.Add(L.T("Exit (stop alarms)"),null,delegate{exit=true;Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{Restore();};
  FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!exit&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,L.T("AlarmApp"),L.T("Running in the tray. Right-click the icon to exit."),ToolTipIcon.Info);}else{Finish();}};
  if(File.Exists(file))using(var f=File.OpenRead(file))alarms=(List<Alarm>)new XmlSerializer(typeof(List<Alarm>)).Deserialize(f);
  history=new UsageHistory(Path.Combine(directory,"usage-history.xml"));if(!string.IsNullOrEmpty(history.Error))MessageBox.Show(this,L.Message(history.Error),L.T("Usage history"));DateTime now=DateTime.Now;foreach(var a in alarms){a.Name=LegacyText.Convert(a.Name);a.SoundName=LegacyText.Convert(a.SoundName);a.Initialize(now,false);history.StartUsage(a,now);}history.Save(now);
  ShowPage(0);Arm();timer.Interval=1000;timer.Tick+=Tick;timer.Start();
 }
 
 void ApplyLanguage(){
  DateTime date=picker!=null&&!picker.IsDisposed?picker.Value:DateTime.Now;int type=typeFilter!=null&&!typeFilter.IsDisposed?typeFilter.SelectedIndex:0,kind=recordFilter!=null&&!recordFilter.IsDisposed?recordFilter.SelectedIndex:0;
  Text=L.T("AlarmApp");brandLabel.Text=L.T("AlarmApp");sideNote.Text=L.T("Close to tray.\nAlarms keep running.");languageLabel.Text=L.T("Language");string[] labels={"My alarms","Summary","History"};for(int i=0;i<3;i++)nav[i].Text=L.T(labels[i]);
  tray.Text=L.T("AlarmApp");var menu=tray.ContextMenuStrip;menu.Items.Clear();menu.Items.Add(L.T("Open AlarmApp"),null,delegate{Restore();});menu.Items.Add(L.T("Exit (stop alarms)"),null,delegate{exit=true;Close();});
  ShowPage(page);if(page!=0){picker.Value=date;typeFilter.SelectedIndex=type;if(recordFilter!=null)recordFilter.SelectedIndex=kind;RenderStats();}
  foreach(var pair in rings){pair.Value.Text=L.T("Alarm ringing");Alarm a=alarms.FirstOrDefault(x=>x.Id==pair.Key);if(a!=null)pair.Value.Controls[0].Text=RingText(a);pair.Value.Controls[1].Text=L.T("Stop");pair.Value.Controls[2].Text=L.T("Snooze 5 minutes");}
 }
 void Restore(){Show();WindowState=FormWindowState.Normal;Activate();}
 void ShowPage(int index){page=index;clock=null;nextLabels.Clear();if(pageBody!=null){content.Controls.Remove(pageBody);pageBody.Dispose();}pageBody=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Bg};content.Controls.Add(pageBody);pageBody.BringToFront();for(int i=0;i<3;i++){nav[i].BackColor=i==page?Color.FromArgb(232,239,255):Color.White;nav[i].ForeColor=i==page?Theme.Blue:Theme.Muted;}
  if(page==0)BuildAlarms();else BuildHistory(page==1);Arm();
 }
 void BuildAlarms(){
  Panel header=new Panel{Dock=DockStyle.Top,Height=95};header.Controls.Add(Theme.Label(L.T("My alarms"),0,0,250,38,22,true));var sub=Theme.Label(L.T("Plan reminders and keep your rhythm."),0,44,330,27,10,false);sub.ForeColor=Theme.Muted;header.Controls.Add(sub);
  Button add=Theme.Button(L.T("+ Add alarm"),()=>Edit(null),true);add.SetBounds(Math.Max(420,pageBody.ClientSize.Width-145),10,140,42);add.Anchor=AnchorStyles.Top|AnchorStyles.Right;header.Controls.Add(add);
  clock=Theme.Label(DateTime.Now.ToString("HH:mm:ss"),Math.Max(260,pageBody.ClientSize.Width-285),10,130,35,17,true);clock.Anchor=AnchorStyles.Top|AnchorStyles.Right;header.Controls.Add(clock);
  cards=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(0,5,0,0),BackColor=Theme.Bg};pageBody.Controls.Add(cards);pageBody.Controls.Add(header);cards.Resize+=delegate{foreach(Control c in cards.Controls)c.Width=Math.Max(600,cards.ClientSize.Width-24);};RefreshCards();
 }
 void RefreshCards(){if(page!=0||cards==null||cards.IsDisposed)return;int scroll=cards.VerticalScroll.Value;foreach(Control c in cards.Controls.Cast<Control>().ToArray())c.Dispose();cards.Controls.Clear();nextLabels.Clear();
  if(alarms.Count==0){Card empty=new Card{Width=Math.Max(600,cards.ClientSize.Width-24),Height=260,Margin=new Padding(0,0,0,16)};empty.Controls.Add(Theme.Label(L.T("No alarms yet"),30,45,440,40,20,true));var help=Theme.Label(L.T("Create a scheduled reminder or an interval task."),30,94,480,35,11,false);help.ForeColor=Theme.Muted;empty.Controls.Add(help);var add=Theme.Button(L.T("Create your first alarm"),()=>Edit(null),true);add.SetBounds(30,155,175,42);empty.Controls.Add(add);cards.Controls.Add(empty);return;}
  foreach(var a in alarms.OrderByDescending(x=>x.Enabled).ThenBy(x=>x.Hour*60+x.Minute)){
   Card card=new Card{Width=Math.Max(600,cards.ClientSize.Width-24),Height=172,Margin=new Padding(0,0,0,14)};
   card.Controls.Add(Theme.Label(a.Name,22,15,310,30,13,true));card.Controls.Add(Theme.Label(a.IsInterval?L.T("Every ")+a.IntervalMinutes+L.T(" min"):string.Format("{0:00}:{1:00}:{2:00}",a.Hour,a.Minute,a.Second),22,49,310,45,24,true));
   var repeat=Theme.Label(Repeat(a),22,99,350,25,10,false);repeat.ForeColor=Theme.Muted;card.Controls.Add(repeat);var sound=Theme.Label(L.T("Sound: ")+L.Sound(a.SoundName),22,132,320,23,9,false);sound.ForeColor=Theme.Muted;card.Controls.Add(sound);
   Label next=Theme.Label(NextText(a),card.Width-280,58,255,46,11,false);next.ForeColor=a.Enabled?Theme.Blue:Theme.Muted;next.Anchor=AnchorStyles.Top|AnchorStyles.Right;card.Controls.Add(next);nextLabels[a.Id]=next;
   Button toggle=Theme.Button(a.Enabled?L.T("Enabled"):L.T("Disabled"),()=>Toggle(a),a.Enabled);toggle.SetBounds(card.Width-112,14,90,32);toggle.Anchor=AnchorStyles.Top|AnchorStyles.Right;card.Controls.Add(toggle);
   FlowLayoutPanel actions=new FlowLayoutPanel{Left=card.Width-272,Top=119,Width=252,Height=38,Anchor=AnchorStyles.Top|AnchorStyles.Right};var edit=Theme.Button(L.T("Edit"),()=>Edit(a),false);edit.Width=72;var test=Theme.Button(L.T("Preview"),()=>Ring(new Alarm{Name=L.T("Test: ")+a.Name,SoundPath=a.SoundPath,SoundName=a.SoundName}),false);test.Width=72;var remove=Theme.Button(L.T("Delete"),()=>Delete(a),false);remove.Width=72;actions.Controls.AddRange(new Control[]{edit,test,remove});card.Controls.Add(actions);cards.Controls.Add(card);
  }
  if(scroll>0)cards.AutoScrollPosition=new Point(0,scroll);
 }
 string Repeat(Alarm a){if(a.IsInterval)return L.T("Interval - fired ")+a.FiredCount+" / "+a.TotalCount+L.T(" times");if(a.Days==0)return L.T("Scheduled - once");if(a.Days==127)return L.T("Scheduled - daily");return L.T("Scheduled - ")+string.Join(L.T(", "),Enumerable.Range(0,7).Where(i=>(a.Days&(1<<i))!=0).Select(i=>new[]{L.T("Sun"),L.T("Mon"),L.T("Tue"),L.T("Wed"),L.T("Thu"),L.T("Fri"),L.T("Sat")}[i]).ToArray());}
 string NextText(Alarm a){DateTime d;if(snoozes.ContainsKey(a.Id))d=snoozes[a.Id];else if(a.Enabled)d=a.Due;else return a.IsInterval&&a.FiredCount>=a.TotalCount?L.T("Completed"):L.T("Disabled");double seconds=Math.Max(0,(d-DateTime.Now).TotalSeconds);return L.T("Next: ")+d.ToString("MM-dd HH:mm:ss")+(a.IsInterval?L.T("\nIn ")+UsageHistory.Duration(seconds):"");}
 void Toggle(Alarm a){DateTime now=DateTime.Now;a.Enabled=!a.Enabled;snoozes.Remove(a.Id);if(a.Enabled){a.Initialize(now,true);history.StartUsage(a,now);}else{history.StopUsage(a.Id,now);if(rings.ContainsKey(a.Id))rings[a.Id].Close();}Changed();}
 void Delete(Alarm a){if(MessageBox.Show(this,L.T("Delete \"")+a.Name+L.T("\"? History will be kept."),L.T("Delete alarm"),MessageBoxButtons.YesNo)!=DialogResult.Yes)return;history.StopUsage(a.Id,DateTime.Now);alarms.Remove(a);snoozes.Remove(a.Id);if(rings.ContainsKey(a.Id))rings[a.Id].Close();Changed();}
 void Edit(Alarm a){using(Editor e=new Editor(a))if(e.ShowDialog(this)==DialogResult.OK){DateTime now=DateTime.Now;if(a!=null){history.StopUsage(a.Id,now);alarms.Remove(a);snoozes.Remove(a.Id);if(rings.ContainsKey(a.Id))rings[a.Id].Close();}e.Result.Initialize(now,true);alarms.Add(e.Result);history.StartUsage(e.Result,now);Changed();}}
 void Save(){try{Directory.CreateDirectory(directory);using(var f=File.Create(file+".tmp"))new XmlSerializer(typeof(List<Alarm>)).Serialize(f,alarms);if(File.Exists(file))File.Replace(file+".tmp",file,file+".bak");else File.Move(file+".tmp",file);}catch(Exception e){MessageBox.Show(this,L.T("Settings save failed: ")+e.Message,L.T("Alarm"));}}
 void Changed(){Save();history.Save(DateTime.Now);RefreshCards();if(page!=0)RenderStats();Arm();}
 void Arm(){DateTime d=DateTime.MaxValue;foreach(var a in alarms.Where(x=>x.Enabled))if(a.Due<d)d=a.Due;foreach(var s in snoozes.Values)if(s<d)d=s;bool ok=wake.Arm(d);status.Text=!string.IsNullOrEmpty(history.Error)?L.Message(history.Error):(d==DateTime.MaxValue?L.T("No pending alarms"):L.T("Next alarm: ")+d.ToString("MM-dd HH:mm:ss"))+L.T(" | Close to tray")+(ok?"":L.T(" | Wake request failed"));}
 void Tick(object sender,EventArgs e){DateTime now=DateTime.Now;if(clock!=null&&!clock.IsDisposed)clock.Text=now.ToString("HH:mm:ss");bool changed=false;foreach(var a in alarms.ToArray())if(a.Enabled&&a.Due<=now){history.Trigger(a);a.Fire(now);Ring(a);if(!a.Enabled)history.StopUsage(a.Id,now);changed=true;}foreach(var id in snoozes.Keys.ToArray())if(snoozes[id]<=now){snoozes.Remove(id);var a=alarms.FirstOrDefault(x=>x.Id==id);if(a!=null)Ring(a);changed=true;}foreach(var a in alarms)if(nextLabels.ContainsKey(a.Id))nextLabels[a.Id].Text=NextText(a);if(changed)Changed();if(++ticks%30==0){history.Save(now);if(page!=0)RenderStats();Arm();}}
 string RingText(Alarm a){return a.Name+"\n"+DateTime.Now.ToString("HH:mm:ss")+(a.IsInterval?string.Format(L.T("  Ring {0}/{1}"),a.FiredCount,a.TotalCount):"");}
 void Ring(Alarm a){if(rings.ContainsKey(a.Id)){rings[a.Id].Controls[0].Text=RingText(a);return;}bool tracked=alarms.Any(x=>x.Id==a.Id);if(tracked)history.StartRing(a,DateTime.Now);
  Form f=new Form{Text=L.T("Alarm ringing"),ClientSize=new Size(440,225),TopMost=true,StartPosition=FormStartPosition.CenterScreen,MaximizeBox=false,MinimizeBox=false};f.Controls.Add(Theme.Label(RingText(a),22,22,395,120,18,true));var stop=Theme.Button(L.T("Stop"),()=>f.Close(),true);stop.SetBounds(25,165,145,38);f.Controls.Add(stop);var later=Theme.Button(L.T("Snooze 5 minutes"),()=>{snoozes[a.Id]=DateTime.Now.AddMinutes(5);f.Close();RefreshCards();Arm();},false);later.Enabled=tracked;later.SetBounds(190,165,215,38);f.Controls.Add(later);Theme.Style(f);rings[a.Id]=f;
  f.FormClosed+=delegate{rings.Remove(a.Id);if(players.ContainsKey(a.Id)){players[a.Id].Dispose();players.Remove(a.Id);}if(tracked){history.StopRing(a.Id,DateTime.Now);history.Save(DateTime.Now);}if(page!=0&&!finished)RenderStats();};f.Show();f.Activate();try{TonePlayer player=new TonePlayer();players[a.Id]=player;try{player.Load(a.SoundPath);player.Play();}catch(Exception error){player.Dispose();f.Controls[0].Text=RingText(a)+L.T("\nSound unavailable; using Classic.");try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audio-error.log"),error.ToString()+Environment.NewLine);}catch{}player.Load("alarm.wav");player.Play();}}catch{SystemSounds.Exclamation.Play();}if(tracked)history.Save(DateTime.Now);
 }
 void BuildHistory(bool summary){
  Panel header=new Panel{Dock=DockStyle.Top,Height=118};header.Controls.Add(Theme.Label(summary?L.T("Summary"):L.T("History"),0,0,270,40,22,true));var caption=Theme.Label(summary?L.T("Monthly usage at a glance."):L.T("Daily usage and ringing sessions."),0,45,650,26,10,false);caption.ForeColor=Theme.Muted;header.Controls.Add(caption);
  picker=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat=summary?L.T("yyyy-MM"):L.T("yyyy-MM-dd"),ShowUpDown=summary,Left=0,Top=80,Width=185};header.Controls.Add(picker);
  typeFilter=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Left=200,Top=80,Width=145};typeFilter.Items.AddRange(new object[]{L.T("All types"),L.T("Scheduled"),L.T("Interval")});typeFilter.SelectedIndex=0;header.Controls.Add(typeFilter);
  recordFilter=null;if(!summary){recordFilter=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Left=360,Top=80,Width=140};recordFilter.Items.AddRange(new object[]{L.T("All records"),L.T("Usage"),L.T("Ringing")});recordFilter.SelectedIndex=0;header.Controls.Add(recordFilter);recordFilter.SelectedIndexChanged+=delegate{RenderStats();};}
  var refresh=Theme.Button(L.T("Refresh"),()=>{history.Save(DateTime.Now);RenderStats();},false);refresh.SetBounds(summary?365:515,77,90,33);header.Controls.Add(refresh);
  statsBody=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Theme.Bg};pageBody.Controls.Add(statsBody);pageBody.Controls.Add(header);picker.ValueChanged+=delegate{RenderStats();};typeFilter.SelectedIndexChanged+=delegate{RenderStats();};RenderStats();
 }
 IEnumerable<UsageEntry> Filtered(){string type=L.TypeKey(typeFilter.SelectedIndex);return history.Entries.Where(x=>type==null||x.AlarmType==type);}
 void RenderStats(){if(page==0||statsBody==null||statsBody.IsDisposed)return;foreach(var c in statsBody.Controls.Cast<Control>().ToArray())c.Dispose();statsBody.Controls.Clear();DateTime now=DateTime.Now,from=page==1?new DateTime(picker.Value.Year,picker.Value.Month,1):picker.Value.Date,to=page==1?from.AddMonths(1):from.AddDays(1);var entries=Filtered().Where(x=>UsageHistory.Seconds(x,from,to,now)>0).ToList();double usage=entries.Where(x=>x.Kind=="Usage").Sum(x=>UsageHistory.Seconds(x,from,to,now)),ring=entries.Where(x=>x.Kind=="Ringing").Sum(x=>UsageHistory.Seconds(x,from,to,now));int sessions=entries.Count(x=>x.Kind=="Usage");
  FlowLayoutPanel metrics=new FlowLayoutPanel{Dock=DockStyle.Top,Height=112,WrapContents=false};string[] titles={L.T("Usage duration"),L.T("Sessions"),L.T("Ringing duration")},values={UsageHistory.Duration(usage),sessions+L.T(" times"),UsageHistory.Duration(ring)};for(int i=0;i<3;i++){Card c=new Card{Width=230,Height=98,Margin=new Padding(0,0,14,0)};var title=Theme.Label(titles[i],18,15,200,24,10,false);title.ForeColor=Theme.Muted;c.Controls.Add(title);c.Controls.Add(Theme.Label(values[i],18,45,210,35,17,true));metrics.Controls.Add(c);}metrics.Resize+=delegate{foreach(Control c in metrics.Controls)c.Width=Math.Max(180,(metrics.ClientSize.Width-42)/3);};statsBody.Controls.Add(metrics);
  if(page==1){Panel lower=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,12,0,0)};Card chartCard=new Card{Dock=DockStyle.Fill};Label title=Theme.Label(L.T("Daily usage - blue: scheduled / light blue: interval"),18,14,650,30,11,true);chartCard.Controls.Add(title);UsageChart chart=new UsageChart{Dock=DockStyle.Fill,Font=new Font("Microsoft YaHei UI",9),Month=from};int days=DateTime.DaysInMonth(from.Year,from.Month);chart.ClockValues=new double[days];chart.IntervalValues=new double[days];for(int d=0;d<days;d++){DateTime a=from.AddDays(d),b=a.AddDays(1);foreach(var entry in entries.Where(x=>x.Kind=="Usage")){double sec=UsageHistory.Seconds(entry,a,b,now);if(entry.AlarmType=="Interval")chart.IntervalValues[d]+=sec;else chart.ClockValues[d]+=sec;}}Panel chartHost=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,50,12,48)};chartHost.Controls.Add(chart);chartCard.Controls.Add(chartHost);chartHost.SendToBack();Label types=Theme.Label(L.T("Scheduled: ")+UsageHistory.Duration(entries.Where(x=>x.Kind=="Usage"&&x.AlarmType=="Scheduled").Sum(x=>UsageHistory.Seconds(x,from,to,now)))+L.T("    Interval: ")+UsageHistory.Duration(entries.Where(x=>x.Kind=="Usage"&&x.AlarmType=="Interval").Sum(x=>UsageHistory.Seconds(x,from,to,now))),18,0,670,35,10,false);types.Dock=DockStyle.Bottom;chartCard.Controls.Add(types);lower.Controls.Add(chartCard);statsBody.Controls.Add(lower);lower.BringToFront();}
  else{
   DataGridView grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,GridColor=Theme.Line,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,EnableHeadersVisualStyles=false};grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(238,243,253);grid.ColumnHeadersDefaultCellStyle.ForeColor=Theme.Text;grid.ColumnHeadersHeight=40;grid.RowTemplate.Height=40;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(225,235,255);grid.DefaultCellStyle.SelectionForeColor=Theme.Text;foreach(var name in new[]{L.T("Name"),L.T("Alarm type"),L.T("Record"),L.T("Start"),L.T("End"),L.T("Duration")})grid.Columns.Add(name,name);
   string kind=L.RecordKey(recordFilter.SelectedIndex);foreach(var entry in entries.Where(x=>kind==null||x.Kind==kind).OrderByDescending(x=>x.Start)){DateTime start=entry.Start>from?entry.Start:from,end=entry.IsOpen?now:entry.End;if(end>to)end=to;grid.Rows.Add(entry.Name,L.T(entry.AlarmType),L.T(entry.Kind),start.ToString("HH:mm:ss"),end.ToString("HH:mm:ss")+(entry.IsOpen&&to>now?L.T(" (active)"):""),UsageHistory.Duration(UsageHistory.Seconds(entry,from,to,now)));}
   Panel host=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,8,0,0)};host.Controls.Add(grid);statsBody.Controls.Add(host);host.BringToFront();
  }
  Label note=Theme.Label(entries.Count==0?L.T("No records. History starts when this version first runs."):L.T("Each alarm is counted separately. Tests are not recorded."),0,0,700,35,9,false);note.ForeColor=Theme.Muted;note.Dock=DockStyle.Bottom;statsBody.Controls.Add(note);note.SendToBack();
 }
 void Finish(){if(finished)return;finished=true;timer.Stop();foreach(var f in rings.Values.ToArray())f.Close();if(history!=null)history.CloseAll(DateTime.Now);}
 protected override void Dispose(bool disposing){if(disposing){Finish();timer.Dispose();tray.Dispose();wake.Dispose();foreach(var p in players.Values.ToArray())p.Dispose();players.Clear();}base.Dispose(disposing);}
}


static class L {
 public static string Language="en";
 public static string PreferencePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IndependentAlarm","ui-language.txt");
 static readonly Dictionary<string,string> Chinese=new Dictionary<string,string>{{"Alarm","\u95f9\u949f"},
{"Classic","\u7ecf\u5178\u63d0\u793a"},
{"Invalid interval settings.","\u95f4\u9694\u95f9\u949f\u8bbe\u7f6e\u65e0\u6548\u3002"},
{"Audio playback failed: ","\u97f3\u9891\u64ad\u653e\u5931\u8d25\uff1a"},
{"Sound file not found.","\u627e\u4e0d\u5230\u94c3\u58f0\u6587\u4ef6\u3002"},
{"Select a WAV or MP3 file.","\u8bf7\u9009\u62e9 WAV \u6216 MP3 \u97f3\u9891\u3002"},
{"No sound loaded.","\u5c1a\u672a\u52a0\u8f7d\u94c3\u58f0\u3002"},
{"Add alarm","\u6dfb\u52a0\u95f9\u949f"},
{"Edit alarm","\u7f16\u8f91\u95f9\u949f"},
{"Name","\u540d\u79f0"},
{"Scheduled","\u5b9a\u65f6\u95f9\u949f"},
{"Interval (start now)","\u95f4\u9694\u91cd\u590d\uff08\u4ece\u73b0\u5728\u8ba1\u65f6\uff09"},
{"Enabled","\u5df2\u542f\u7528"},
{"Alarm time","\u54cd\u94c3\u65f6\u95f4"},
{"Use current time","\u4f7f\u7528\u5f53\u524d\u65f6\u95f4"},
{"Repeat days (none = once)","\u91cd\u590d\u65e5\u671f\uff08\u4e0d\u52fe\u9009\u8868\u793a\u5355\u6b21\uff09"},
{"Sunday","\u661f\u671f\u65e5"},
{"Monday","\u661f\u671f\u4e00"},
{"Tuesday","\u661f\u671f\u4e8c"},
{"Wednesday","\u661f\u671f\u4e09"},
{"Thursday","\u661f\u671f\u56db"},
{"Friday","\u661f\u671f\u4e94"},
{"Saturday","\u661f\u671f\u516d"},
{"Every","\u6bcf\u9694"},
{"min, total","\u5206\u949f\uff0c\u5171"},
{"times","\u6b21"},
{"Alarm sound","\u54cd\u94c3\u58f0\u97f3"},
{"Soft bell","\u8f7b\u67d4\u949f\u58f0"},
{"Electronic pulse","\u7535\u5b50\u8109\u51b2"},
{"Morning melody","\u6668\u95f4\u65cb\u5f8b"},
{"Urgent","\u6025\u4fc3\u63d0\u9192"},
{"Custom sound","\u81ea\u5b9a\u4e49\u94c3\u58f0"},
{"Import WAV / MP3","\u5bfc\u5165 WAV / MP3"},
{"Preview","\u8bd5\u542c"},
{"Sound preview","\u94c3\u58f0\u8bd5\u542c"},
{"Stop preview","\u505c\u6b62\u8bd5\u542c"},
{"Save","\u4fdd\u5b58"},
{"Cancel","\u53d6\u6d88"},
{"Choose alarm sound","\u9009\u62e9\u95f9\u949f\u94c3\u58f0"},
{"Audio (*.wav;*.mp3)|*.wav;*.mp3|WAV files|*.wav|MP3 files|*.mp3","\u97f3\u9891\u6587\u4ef6 (*.wav;*.mp3)|*.wav;*.mp3|WAV \u6587\u4ef6|*.wav|MP3 \u6587\u4ef6|*.mp3"},
{"Custom: ","\u81ea\u5b9a\u4e49\uff1a"},
{"Cannot import sound: ","\u65e0\u6cd5\u5bfc\u5165\u94c3\u58f0\uff1a"},
{"Import sound","\u94c3\u58f0\u5bfc\u5165"},
{"First ring after one interval.\nSaving or re-enabling restarts the count.","\u4fdd\u5b58\u540e\u4ece\u73b0\u5728\u8ba1\u65f6\uff0c\u7ecf\u8fc7\u4e00\u4e2a\u95f4\u9694\u9996\u6b21\u54cd\u94c3\u3002\n\u7f16\u8f91\u4fdd\u5b58\u6216\u91cd\u65b0\u542f\u7528\u4f1a\u4ece\u7b2c 1 \u6b21\u91cd\u65b0\u8ba1\u65f6\u3002"},
{"Current time is filled automatically.\nPast times move to the next matching day.","\u9ed8\u8ba4\u586b\u5165\u5f53\u524d\u65f6\u95f4\uff0c\u53ef\u7528\u6309\u94ae\u91cd\u65b0\u83b7\u53d6\u3002\n\u5df2\u8fc7\u7684\u65f6\u95f4\u4f1a\u5b89\u6392\u5728\u4e0b\u4e00\u4e2a\u7b26\u5408\u6761\u4ef6\u7684\u65e5\u671f\u3002"},
{"History load failed; original preserved: ","\u4f7f\u7528\u8bb0\u5f55\u8bfb\u53d6\u5931\u8d25\uff0c\u539f\u6587\u4ef6\u4fdd\u7559\uff1a"},
{"h ","\u5c0f\u65f6"},
{"m ","\u5206"},
{"s","\u79d2"},
{"Interval","\u95f4\u9694\u95f9\u949f"},
{"Usage","\u8ba1\u65f6"},
{"Ringing","\u54cd\u94c3"},
{"History save failed: ","\u4f7f\u7528\u8bb0\u5f55\u4fdd\u5b58\u5931\u8d25\uff1a"},
{"Stop","\u505c\u6b62"},
{"AlarmApp","\u72ec\u7acb\u95f9\u949f"},
{"My alarms","\u6211\u7684\u95f9\u949f"},
{"Summary","\u7b80\u8981\u7edf\u8ba1"},
{"History","\u8be6\u7ec6\u8bb0\u5f55"},
{"Close to tray.\nAlarms keep running.","\u5173\u95ed\u7a97\u53e3\u540e\n\u7ee7\u7eed\u5728\u6258\u76d8\u8fd0\u884c"},
{"Open AlarmApp","\u6253\u5f00\u95f9\u949f"},
{"Exit (stop alarms)","\u9000\u51fa\uff08\u505c\u6b62\u63d0\u9192\uff09"},
{"Running in the tray. Right-click the icon to exit.","\u6b63\u5728\u6258\u76d8\u7ee7\u7eed\u63d0\u9192\uff0c\u53f3\u952e\u56fe\u6807\u53ef\u9000\u51fa\u3002"},
{"Usage history","\u4f7f\u7528\u8bb0\u5f55"},
{"Plan reminders and keep your rhythm.","\u5b89\u6392\u63d0\u9192\uff0c\u8ba9\u65f6\u95f4\u66f4\u6709\u8282\u594f"},
{"+ Add alarm","\uff0b \u6dfb\u52a0\u95f9\u949f"},
{"No alarms yet","\u8fd8\u6ca1\u6709\u95f9\u949f"},
{"Create a scheduled reminder or an interval task.","\u521b\u5efa\u5b9a\u65f6\u63d0\u9192\uff0c\u6216\u8bbe\u7f6e\u4e00\u4e2a\u95f4\u9694\u4efb\u52a1\u3002"},
{"Create your first alarm","\u521b\u5efa\u7b2c\u4e00\u4e2a\u95f9\u949f"},
{"Every ","\u6bcf "},
{" min"," \u5206\u949f"},
{"Sound: ","\u94c3\u58f0 \u00b7 "},
{"Disabled","\u5df2\u505c\u7528"},
{"Edit","\u7f16\u8f91"},
{"Test: ","\u6d4b\u8bd5 \u00b7 "},
{"Delete","\u5220\u9664"},
{"Interval - fired ","\u95f4\u9694\u95f9\u949f \u00b7 \u5df2\u54cd "},
{" times"," \u6b21"},
{"Scheduled - once","\u5b9a\u65f6\u95f9\u949f \u00b7 \u5355\u6b21"},
{"Scheduled - daily","\u5b9a\u65f6\u95f9\u949f \u00b7 \u6bcf\u5929"},
{"Scheduled - ","\u5b9a\u65f6\u95f9\u949f \u00b7 \u5468"},
{", ","\u3001"},
{"Sun","\u65e5"},
{"Mon","\u4e00"},
{"Tue","\u4e8c"},
{"Wed","\u4e09"},
{"Thu","\u56db"},
{"Fri","\u4e94"},
{"Sat","\u516d"},
{"Completed","\u672c\u8f6e\u5df2\u5b8c\u6210"},
{"Next: ","\u4e0b\u4e00\u6b21 \u00b7 "},
{"\nIn ","\n\u8fd8\u6709 "},
{"Delete \"","\u5220\u9664\u201c"},
{"\"? History will be kept.","\u201d\uff1f\u4f7f\u7528\u8bb0\u5f55\u4ecd\u4f1a\u4fdd\u7559\u3002"},
{"Delete alarm","\u5220\u9664\u95f9\u949f"},
{"Settings save failed: ","\u8bbe\u7f6e\u4fdd\u5b58\u5931\u8d25\uff1a"},
{"No pending alarms","\u6682\u65e0\u5f85\u54cd\u95f9\u949f"},
{"Next alarm: ","\u4e0b\u6b21\u63d0\u9192 "},
{" | Close to tray","  \u00b7  \u5173\u95ed\u7a97\u53e3\u540e\u7ee7\u7eed\u5728\u6258\u76d8\u8fd0\u884c"},
{" | Wake request failed","  \u00b7  \u7761\u7720\u5524\u9192\u8bf7\u6c42\u5931\u8d25"},
{"  Ring {0}/{1}","  \u7b2c {0}/{1} \u6b21"},
{"Alarm ringing","\u95f9\u949f\u54cd\u4e86"},
{"Snooze 5 minutes","5 \u5206\u949f\u540e\u63d0\u9192"},
{"\nSound unavailable; using Classic.","\n\u94c3\u58f0\u4e0d\u53ef\u7528\uff0c\u5df2\u6539\u7528\u7ecf\u5178\u63d0\u793a\u3002"},
{"Monthly usage at a glance.","\u6309\u6708\u67e5\u770b\u7d2f\u8ba1\u4f7f\u7528\u60c5\u51b5"},
{"Daily usage and ringing sessions.","\u6309\u5929\u67e5\u770b\u6bcf\u6b21\u4f7f\u7528\u4e0e\u54cd\u94c3\u8bb0\u5f55"},
{"yyyy-MM","yyyy\u5e74MM\u6708"},
{"yyyy-MM-dd","yyyy\u5e74MM\u6708dd\u65e5"},
{"All types","\u5168\u90e8\u7c7b\u578b"},
{"All records","\u5168\u90e8\u8bb0\u5f55"},
{"Refresh","\u5237\u65b0"},
{"Usage duration","\u8ba1\u65f6\u4f7f\u7528"},
{"Sessions","\u4f7f\u7528\u6b21\u6570"},
{"Ringing duration","\u54cd\u94c3\u65f6\u957f"},
{"Daily usage - blue: scheduled / light blue: interval","\u6bcf\u65e5\u8ba1\u65f6\u4f7f\u7528  \u00b7  \u6df1\u84dd\uff1a\u5b9a\u65f6  /  \u6d45\u84dd\uff1a\u95f4\u9694"},
{"Scheduled: ","\u5b9a\u65f6\uff1a"},
{"    Interval: ","    \u95f4\u9694\uff1a"},
{"Alarm type","\u95f9\u949f\u7c7b\u578b"},
{"Record","\u8bb0\u5f55"},
{"Start","\u5f00\u59cb"},
{"End","\u7ed3\u675f"},
{"Duration","\u65f6\u957f"},
{" (active)"," \u00b7 \u8fdb\u884c\u4e2d"},
{"No records. History starts when this version first runs.","\u8fd9\u4e2a\u65e5\u671f\u6ca1\u6709\u8bb0\u5f55\u3002\u5386\u53f2\u4ece\u65b0\u7248\u9996\u6b21\u8fd0\u884c\u5f00\u59cb\u79ef\u7d2f\u3002"},
{"Each alarm is counted separately. Tests are not recorded.","\u591a\u4e2a\u95f9\u949f\u5206\u522b\u7d2f\u8ba1\uff1b\u8de8\u65e5\u6309\u65e5\u671f\u5206\u644a\u3002\u6d4b\u8bd5\u54cd\u94c3\u4e0d\u8ba1\u5165\u8bb0\u5f55\u3002"},
{"AlarmApp is already running. Open it from the tray.","\u95f9\u949f\u5df2\u5728\u8fd0\u884c\uff0c\u8bf7\u4ece\u7cfb\u7edf\u6258\u76d8\u6253\u5f00\u3002"},
{"AlarmApp failed:\n","\u95f9\u949f\u8fd0\u884c\u5931\u8d25\uff1a\n"},
{"\nSee runtime-error.log.","\n\u8be6\u60c5\u89c1 runtime-error.log\u3002"},
{"Custom MP3","\u81ea\u5b9a\u4e49 MP3"},
{"Language","\u8bed\u8a00"},
{"Language settings","\u8bed\u8a00\u8bbe\u7f6e"},
{"Could not save language preference.","\u65e0\u6cd5\u4fdd\u5b58\u8bed\u8a00\u8bbe\u7f6e\u3002"},
{"h","\u5c0f\u65f6"}};
 public static string T(string key){if(key==null||Language!="zh-CN")return key;string text;if(Chinese.TryGetValue(key,out text))return text;return key;}
 public static string Sound(string value){if(value!=null&&value.StartsWith("Custom: ",StringComparison.Ordinal))return T("Custom: ")+value.Substring(8);return T(value);}
 public static string Message(string value){if(value==null)return value;foreach(string prefix in new[]{"History load failed; original preserved: ","History save failed: ","Audio playback failed: "})if(value.StartsWith(prefix,StringComparison.Ordinal))return T(prefix)+value.Substring(prefix.Length);return T(value);}
 public static string TypeKey(int index){return index==1?"Scheduled":index==2?"Interval":null;}
 public static string RecordKey(int index){return index==1?"Usage":index==2?"Ringing":null;}
 public static void Load(string path){Language="en";try{if(File.Exists(path)){string code=File.ReadAllText(path).Trim();if(code=="en"||code=="zh-CN")Language=code;}}catch{}}
 public static void Choose(string code,string path){if(code!="en"&&code!="zh-CN")throw new ArgumentException("Unsupported language.");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",code,Encoding.ASCII);if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);Language=code;}
}

static class Program {
 [STAThread] static void Main(string[] args){if(args.Contains("--self-test")){Test();return;}L.Load(L.PreferencePath);bool created;using(Mutex m=new Mutex(true,"Local\\IndependentAlarmDesktop",out created)){if(!created){MessageBox.Show(L.T("AlarmApp is already running. Open it from the tray."));return;}Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){Report(e.Exception);};try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new App());}catch(Exception e){Report(e);Environment.ExitCode=1;}}}
 static void Report(Exception e){try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime-error.log"),e.ToString());}catch{}MessageBox.Show(L.T("AlarmApp failed:\n")+L.Message(e.Message)+L.T("\nSee runtime-error.log."),L.T("AlarmApp"),MessageBoxButtons.OK,MessageBoxIcon.Error);}
 
 
 
 
 static void TestLocalization(){
  string original=L.Language,file=Path.Combine(Path.GetTempPath(),"AlarmAppLanguageTest-"+Guid.NewGuid().ToString("N")+".txt");try{
   L.Choose("zh-CN",file);Check(L.T("My alarms")=="\u6211\u7684\u95f9\u949f","Chinese menu");Check(L.T("Save")=="\u4fdd\u5b58","Chinese editor");Check(L.T("Summary")=="\u7b80\u8981\u7edf\u8ba1"&&L.T("History")=="\u8be6\u7ec6\u8bb0\u5f55","Chinese statistics pages");
   Check(L.TypeKey(1)=="Scheduled"&&L.TypeKey(2)=="Interval"&&L.RecordKey(1)=="Usage","stable data keys");
   L.Language="en";L.Load(file);Check(L.Language=="zh-CN","language preference persisted");L.Choose("en",file);Check(L.T("Save")=="Save"&&L.T("My alarms")=="My alarms","English menu");L.Language="zh-CN";L.Load(file);Check(L.Language=="en","English preference persisted");
   File.WriteAllText(file,"unsupported",Encoding.ASCII);L.Load(file);Check(L.Language=="en","invalid preference fallback");
   Check(L.T("user-defined alarm")=="user-defined alarm","custom names unchanged");
  }finally{L.Language=original;foreach(string path in new[]{file,file+".tmp",file+".bak"})if(File.Exists(path))File.Delete(path);}
 }
 static void TestHistory(){
  DateTime start=new DateTime(2026,8,31,23,58,0),end=start.AddMinutes(5),midnight=new DateTime(2026,9,1);
  UsageEntry session=new UsageEntry{Kind="Usage",Start=start,End=end};Check(UsageHistory.Seconds(session,new DateTime(2026,8,1),midnight,end)==120,"month split august");Check(UsageHistory.Seconds(session,midnight,midnight.AddDays(1),end)==180,"day split september");Check(UsageHistory.Seconds(session,end,end.AddDays(1),end)==0,"end exclusive");
  string file=Path.Combine(Path.GetTempPath(),"IndependentAlarmTest-"+Guid.NewGuid().ToString("N")+".xml");try{
   UsageHistory h=new UsageHistory(file);Alarm a=new Alarm{Name="test",IsInterval=true};h.StartUsage(a,start);h.Trigger(a);h.StartRing(a,start.AddSeconds(110));h.StopRing(a.Id,start.AddSeconds(130));h.StopUsage(a.Id,end);h.Save(end);Check(string.IsNullOrEmpty(h.Error),"history save");var reload=new UsageHistory(file);Check(reload.Entries.Count==2,"history roundtrip");Check(reload.Entries.First(x=>x.Kind=="Usage").RingCount==1,"history trigger count");var r=reload.Entries.First(x=>x.Kind=="Ringing");Check(UsageHistory.Seconds(r,start,midnight,end)==10&&UsageHistory.Seconds(r,midnight,end,end)==10,"ring day split");
   h.StartUsage(a,end);h.Save(end.AddSeconds(30));reload=new UsageHistory(file);var checkpoint=reload.Entries.Last();Check(!checkpoint.IsOpen&&checkpoint.End==end.AddSeconds(30),"crash checkpoint closes without adding downtime");h.CloseAll(end.AddSeconds(45));Check(!h.Entries.Last().IsOpen,"exit ends usage");
  }finally{foreach(var p in new[]{file,file+".bak",file+".tmp"})if(File.Exists(p))File.Delete(p);}
  using(Icon icon=new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AppIcon-v1.ico")))Check(icon.Width>0,"app icon load");
 }
 static void TestAudio(){
  Alarm a=new Alarm{SoundPath="C:\\custom sound.mp3",SoundName="Custom MP3"};var xml=new XmlSerializer(typeof(Alarm));var writer=new StringWriter();xml.Serialize(writer,a);var copy=(Alarm)xml.Deserialize(new StringReader(writer.ToString()));Check(copy.SoundPath==a.SoundPath&&copy.SoundName==a.SoundName,"ringtone selection persistence");
  var legacy=(Alarm)xml.Deserialize(new StringReader("<Alarm><Name>old</Name></Alarm>"));Check(legacy.SoundPath=="alarm.wav"&&legacy.SoundName=="Classic","old ringtone defaults");
  foreach(string asset in new[]{"alarm.wav","sounds/bell.wav","sounds/pulse.wav","sounds/melody.wav","sounds/urgent.wav"})using(TonePlayer player=new TonePlayer())player.Load(asset);
 }
 static void TestInterval(){
  DateTime start=new DateTime(2026,10,11,12,34,56);
  Alarm a=new Alarm{IsInterval=true,IntervalMinutes=5,TotalCount=12};a.Initialize(start,true);
  Check(a.Due==start.AddMinutes(5)&&a.FiredCount==0,"interval first due");
  for(int i=1;i<=12;i++){
   DateTime due=a.Due;Check(due==start.AddMinutes(5*i),"interval fixed spacing");a.Fire(due);
   Check(a.FiredCount==i,"interval count");Check(a.Enabled==(i<12),"interval auto stop");
  }
  Check(a.Due==DateTime.MaxValue,"interval exhausted");
  a.Enabled=true;a.Initialize(start,true);Check(a.FiredCount==0&&a.Due==start.AddMinutes(5),"re-enable reset");
  a.Fire(a.Due);
  var xml=new XmlSerializer(typeof(List<Alarm>));var text=new StringWriter();xml.Serialize(text,new List<Alarm>{a});
  var saved=((List<Alarm>)xml.Deserialize(new StringReader(text.ToString())))[0];saved.Initialize(start.AddMinutes(6),false);
  Check(saved.FiredCount==1&&saved.Due==start.AddMinutes(10),"restart preserves count and future due");
  saved.Initialize(start.AddHours(1),false);Check(saved.FiredCount==1&&saved.Due==start.AddHours(1).AddMinutes(5),"closed period not counted");
  a.Fire(start.AddHours(2));Check(a.FiredCount==2&&a.Due==start.AddHours(2).AddMinutes(5),"sleep one catch-up");
  var legacy=(Alarm)new XmlSerializer(typeof(Alarm)).Deserialize(new StringReader("<Alarm><Name>old</Name><Hour>8</Hour><Minute>30</Minute><Days>0</Days><Enabled>true</Enabled></Alarm>"));
  Check(!legacy.IsInterval&&legacy.Second==0&&legacy.IntervalMinutes==5,"legacy defaults");
  Alarm clock=new Alarm{Hour=12,Minute=35,Second=7};Check(clock.Next(start)==start.Date.AddHours(12).AddMinutes(35).AddSeconds(7),"clock second precision");
 }
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static void Test(){TestLocalization();TestHistory();TestAudio();TestInterval();DateTime monday=new DateTime(2026,10,12,8,0,0);Alarm a=new Alarm();Check(a.Next(monday)==monday.AddDays(1),"single next day");a.Days=2;Check(a.Next(monday)==monday.AddDays(7),"weekly wrap");a.Days=127;Check(a.Next(monday.AddSeconds(-1))==monday,"same day");a.Days=62;Check(a.Next(new DateTime(2026,10,16,9,0,0))==new DateTime(2026,10,19,8,0,0),"weekdays");var x=new XmlSerializer(typeof(List<Alarm>));var w=new StringWriter();x.Serialize(w,new List<Alarm>{a});var copy=(List<Alarm>)x.Deserialize(new StringReader(w.ToString()));Check(copy[0].Id==a.Id&&copy[0].Days==62,"storage");using(Wake wake=new Wake())Check(wake.Arm(DateTime.Now.AddMinutes(10)),"wake API");File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verification.txt"),"PASS: Chinese/English localization and persisted preference, language-independent data keys, history persistence, cross-day and cross-month usage/ring split, crash checkpoints, app icon loading, built-in ringtone loading, ringtone persistence and legacy defaults, interval timing and 12-count completion, restart persistence, overdue recovery, re-enable reset, legacy XML compatibility, second precision, single alarm, weekly repeat, weekdays, XML round trip, Windows wake timer API.\r\n");}
}