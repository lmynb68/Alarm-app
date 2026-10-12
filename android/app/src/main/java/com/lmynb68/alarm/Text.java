package com.lmynb68.alarm;
import android.content.Context;
import java.util.Locale;
final class Text {
 static boolean chinese(Context c){String s=c.getSharedPreferences("prefs",0).getString("language","");return s.isEmpty()?Locale.getDefault().getLanguage().equals("zh"):s.equals("zh");}
 static String t(Context c,String en,String zh){return chinese(c)?zh:en;}
 static String type(Context c,String value){return value.equals("Interval")?t(c,"Interval","间隔闹钟"):t(c,"Scheduled","定时闹钟");}
 static String kind(Context c,String value){return value.equals("Usage")?t(c,"Enabled time","启用时长"):t(c,"Ringing","响铃");}
 static String duration(long ms){long seconds=ms/1000;return String.format(Locale.ROOT,"%02d:%02d:%02d",seconds/3600,seconds/60%60,seconds%60);}
}
