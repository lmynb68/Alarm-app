package com.lmynb68.alarm;
import android.content.Context;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
final class Store {
 static final class Data { final ArrayList<Alarm> alarms=new ArrayList<>(); final JSONArray history=new JSONArray(); }
 static synchronized Data load(Context c) {
  Data d=new Data();AtomicFile file=new AtomicFile(new File(c.getFilesDir(),"alarms.json"));
  if(!file.getBaseFile().exists()&&!new File(file.getBaseFile()+".bak").exists())return d;
  try(InputStream in=file.openRead()){
   ByteArrayOutputStream bytes=new ByteArrayOutputStream();byte[] b=new byte[8192];int n;while((n=in.read(b))!=-1)bytes.write(b,0,n);
   JSONObject root=new JSONObject(bytes.toString("UTF-8"));JSONArray list=root.getJSONArray("alarms");for(int i=0;i<list.length();i++)d.alarms.add(Alarm.read(list.getJSONObject(i)));
   JSONArray history=root.getJSONArray("history");for(int i=0;i<history.length();i++)d.history.put(history.getJSONObject(i));return d;
  }catch(Exception e){throw new IllegalStateException("Cannot read saved alarms; data has not been overwritten",e);}
 }
 static synchronized void save(Context c,Data d){
  AtomicFile file=new AtomicFile(new File(c.getFilesDir(),"alarms.json"));FileOutputStream out=null;
  try{JSONArray list=new JSONArray();for(Alarm a:d.alarms)list.put(a.json());byte[] bytes=new JSONObject().put("version",1).put("alarms",list).put("history",d.history).toString().getBytes(StandardCharsets.UTF_8);out=file.startWrite();out.write(bytes);file.finishWrite(out);}
  catch(Exception e){if(out!=null)file.failWrite(out);throw new IllegalStateException("Cannot save alarms",e);}
 }
 static Alarm find(Data d,String id){for(Alarm a:d.alarms)if(a.id.equals(id))return a;return null;}
 static void open(Data d,Alarm a,String kind,String token,long now){
  try{d.history.put(new JSONObject().put("alarm",a.id).put("name",a.name).put("type",a.type()).put("kind",kind).put("token",token).put("start",now).put("end",now).put("open",true).put("count",0));}catch(JSONException e){throw new IllegalStateException(e);}
 }
 static void close(Data d,String id,String kind,long now){for(int i=0;i<d.history.length();i++){JSONObject j=d.history.optJSONObject(i);if(j.optString("alarm").equals(id)&&j.optString("kind").equals(kind)&&j.optBoolean("open")){put(j,"end",Math.max(j.optLong("start"),now));put(j,"open",false);}}}
 static void closeToken(Data d,String token,long now){for(int i=0;i<d.history.length();i++){JSONObject j=d.history.optJSONObject(i);if(j.optString("token").equals(token)&&j.optBoolean("open")){put(j,"end",Math.max(j.optLong("start"),now));put(j,"open",false);}}}
 static void count(Data d,String id){for(int i=d.history.length()-1;i>=0;i--){JSONObject j=d.history.optJSONObject(i);if(j.optString("alarm").equals(id)&&j.optString("kind").equals("Usage")&&j.optBoolean("open")){put(j,"count",j.optInt("count")+1);break;}}}
 static void put(JSONObject j,String k,Object v){try{j.put(k,v);}catch(JSONException e){throw new IllegalStateException(e);}}
 static void recoverRings(Data d){for(int i=0;i<d.history.length();i++){JSONObject j=d.history.optJSONObject(i);if(j.optString("kind").equals("Ringing")&&j.optBoolean("open"))put(j,"open",false);}}
}
