package net.razorvine.pickle.objects;

import java.util.HashMap;
import java.util.TimeZone;
import net.razorvine.pickle.PickleException;

/**
 * Timezone offset class that implements __setstate__ for the unpickler
 * to track what TimeZone a dateutil.tz.tzoffset or tzutc should unpickle to
 */
public class Tzinfo {

    private boolean forceTimeZone;
    private TimeZone timeZone;

    public Tzinfo(TimeZone timeZone) {
        this.forceTimeZone = true;
        this.timeZone = timeZone;
    }

    public Tzinfo() {
        this.forceTimeZone = false;
    }

    public TimeZone getTimeZone() {
        return this.timeZone;
    }

    /**
     * called by the unpickler to restore state
     */
    public void __setstate__(HashMap<String,Object> args) {
        if (this.forceTimeZone)
            return;
        throw new PickleException("unexpected pickle data for tzinfo objects: can't __setstate__ with anything other than an empty dict, anything else is unimplemented");
    }
}
