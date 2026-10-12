package com.lmynb68.alarm;
import android.content.Context;
import android.media.*;
import android.net.Uri;
import java.io.File;
final class Audio {
 static void applyVolume(Context c,MediaPlayer player){if(player!=null){float volume=Math.max(0,Math.min(100,c.getSharedPreferences("prefs",0).getInt("ring-volume",100)))/100f;player.setVolume(volume,volume);}}
 static MediaPlayer create(Context c,String sound)throws Exception{
  MediaPlayer p=new MediaPlayer();p.setAudioAttributes(new AudioAttributes.Builder().setUsage(AudioAttributes.USAGE_ALARM).setContentType(AudioAttributes.CONTENT_TYPE_MUSIC).build());
  try{if(sound.startsWith("custom:")){String name=sound.substring(7);if(!name.matches("[a-f0-9-]+\\.(wav|mp3)"))throw new IllegalArgumentException("Invalid sound");p.setDataSource(new File(new File(c.getFilesDir(),"tones"),name).getAbsolutePath());}
   else {int res;switch(sound){case "bell":res=R.raw.bell;break;case "pulse":res=R.raw.pulse;break;case "melody":res=R.raw.melody;break;case "urgent":res=R.raw.urgent;break;default:res=R.raw.classic;}p.setDataSource(c,Uri.parse("android.resource://"+c.getPackageName()+"/"+res));}
   p.setLooping(true);p.prepare();applyVolume(c,p);return p;
  }catch(Exception e){p.release();throw e;}
 }
}
