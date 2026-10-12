package com.lmynb68.alarm;
import android.app.Application;
public final class AlarmApplication extends Application {
 @Override public void onCreate(){super.onCreate();try{synchronized(Store.class){Store.Data d=Store.load(this);Store.recoverRings(d);Store.save(this,d);}}catch(Exception e){Errors.notify(this,e);}}
}
