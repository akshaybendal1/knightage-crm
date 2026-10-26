package com.knightage.crm;

import android.content.Context;
import android.net.ConnectivityManager;
import android.net.NetworkCapabilities;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebView;
import android.widget.Button;
import androidx.activity.OnBackPressedCallback;
import androidx.core.splashscreen.SplashScreen;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;
import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity implements OfflineWebViewClient.Listener {

    private View offlineLayout;
    private SwipeRefreshLayout swipeRefresh;

    // Set on a failed main-frame load and read back in onMainFrameLoaded --
    // WebView still fires onPageFinished after a failed navigation, so without
    // this the offline screen we just showed would be hidden again immediately.
    // Reset on every onMainFrameLoadStarted so it never lingers into a later,
    // successful attempt.
    private boolean lastLoadFailed = false;

    @Override
    public void onCreate(Bundle savedInstanceState) {
        // @capacitor/push-notifications' register() calls straight into
        // FirebaseMessaging.getInstance(), which throws IllegalStateException on any
        // install that doesn't have google-services.json (Firebase project setup is a
        // manual, user-side step -- see the push notifications plan -- not yet done as
        // of this build). That throw happens on Capacitor's own background
        // "CapacitorPlugins" thread, and push-notifications.ts's try/catch around
        // Fcm.register() can't catch it: Capacitor's Bridge.callPluginMethod doesn't
        // convert this particular failure into a rejected JS promise, it lets the
        // exception escape and crash the whole process -- confirmed by reproducing it
        // (whole app force-closes the moment notification permission is granted, taking
        // down every feature, not just push). Push notifications not working yet is
        // fine; the entire app being unusable because of it is not -- install a handler
        // that swallows only this specific, expected failure and defers everything else
        // to the platform's normal crash handling.
        Thread.UncaughtExceptionHandler previousHandler = Thread.getDefaultUncaughtExceptionHandler();
        Thread.setDefaultUncaughtExceptionHandler((thread, throwable) -> {
            if (isMissingFirebaseConfigError(throwable)) {
                Log.w("MainActivity", "Push notification registration failed because Firebase isn't configured "
                    + "(no google-services.json) -- ignoring so the rest of the app keeps working.", throwable);
                return;
            }
            if (previousHandler != null) {
                previousHandler.uncaughtException(thread, throwable);
            }
        });

        // Must run before super.onCreate() -- otherwise core-splashscreen never
        // reads AppTheme.NoActionBarLaunch's windowSplashScreen* attributes, and
        // Android 12+ silently falls back to its own bare-launcher-icon splash
        // instead of the branded one in res/drawable/splash.xml.
        SplashScreen.installSplashScreen(this);
        super.onCreate(savedInstanceState);

        // super.onCreate() has already built the Bridge and its WebView, inflated
        // from @capacitor/android's own capacitor_bridge_layout_main.xml -- this
        // app's res/layout/activity_main.xml is unused boilerplate in Capacitor 8,
        // so the SwipeRefreshLayout wrapper and the offline overlay both have to
        // be spliced into the existing view tree here instead of declared in XML.
        WebView webView = this.bridge.getWebView();
        ViewGroup root = (ViewGroup) webView.getParent();
        int webViewIndex = root.indexOfChild(webView);
        root.removeView(webView);

        swipeRefresh = new SwipeRefreshLayout(this);
        swipeRefresh.addView(webView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        swipeRefresh.setOnRefreshListener(this::reload);
        root.addView(swipeRefresh, webViewIndex, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));

        offlineLayout = LayoutInflater.from(this).inflate(R.layout.offline_view, root, false);
        root.addView(offlineLayout);
        Button retryButton = offlineLayout.findViewById(R.id.retry_button);
        retryButton.setOnClickListener(v -> reload());

        webView.setWebViewClient(new OfflineWebViewClient(this.bridge, this));

        // Angular's router pushes onto the WebView's own history stack (standard
        // History API pushState), so canGoBack()/goBack() already walk in-app
        // navigation correctly with no web-app changes needed. At the root --
        // nothing left to go back to -- background the app rather than finish()
        // it, matching how most Android apps behave on a top-level screen.
        getOnBackPressedDispatcher().addCallback(this, new OnBackPressedCallback(true) {
            @Override
            public void handleOnBackPressed() {
                if (webView.canGoBack()) {
                    webView.goBack();
                } else {
                    moveTaskToBack(true);
                }
            }
        });

        if (!isNetworkAvailable()) {
            showOffline();
        }
    }

    private void reload() {
        if (isNetworkAvailable()) {
            this.bridge.getWebView().reload();
        } else {
            swipeRefresh.setRefreshing(false);
            showOffline();
        }
    }

    private void showOffline() {
        swipeRefresh.setRefreshing(false);
        offlineLayout.setVisibility(View.VISIBLE);
    }

    // Narrow on purpose -- only swallows the exact known "Firebase isn't configured"
    // shape (walking the full cause chain, since Capacitor wraps the real
    // IllegalStateException in a RuntimeException before it reaches here), so a genuine
    // bug elsewhere still crashes and gets reported normally rather than being hidden.
    private static boolean isMissingFirebaseConfigError(Throwable throwable) {
        for (Throwable cause = throwable; cause != null; cause = cause.getCause()) {
            if (cause instanceof IllegalStateException
                && cause.getMessage() != null
                && cause.getMessage().contains("FirebaseApp is not initialized")) {
                return true;
            }
        }
        return false;
    }

    private boolean isNetworkAvailable() {
        ConnectivityManager connectivityManager = (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        if (connectivityManager == null) {
            return false;
        }
        NetworkCapabilities capabilities = connectivityManager.getNetworkCapabilities(connectivityManager.getActiveNetwork());
        return capabilities != null
            && (capabilities.hasTransport(NetworkCapabilities.TRANSPORT_WIFI)
                || capabilities.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR)
                || capabilities.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET));
    }

    @Override
    public void onMainFrameLoadStarted() {
        lastLoadFailed = false;
    }

    @Override
    public void onMainFrameError() {
        lastLoadFailed = true;
        showOffline();
    }

    @Override
    public void onMainFrameLoaded() {
        swipeRefresh.setRefreshing(false);
        if (!lastLoadFailed) {
            offlineLayout.setVisibility(View.GONE);
        }
    }
}
