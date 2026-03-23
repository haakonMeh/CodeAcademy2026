import { FeedContainer } from "./components/FeedContainer";

// This is the main page of the application. It renders the header and the feed container.
export default function Home() {
  return (
    <div className="min-h-screen">
      <main className="mx-auto max-w-2xl px-4 py-8 sm:px-6 lg:px-8">
        <header className="mb-10 text-center sm:text-left">
          <div className="inline-flex items-center gap-3 mb-2">
            <div className="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg shadow-indigo-500/25">
              <span className="text-xl text-white font-bold">I</span>
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold bg-gradient-to-r from-slate-900 to-slate-700 bg-clip-text text-transparent">
              Idempotweet
            </h1>
          </div>
          <p className="text-slate-500 font-medium">
            Idempotent tweets for reliable systems
          </p>
        </header>
        <FeedContainer />
      </main>
    </div>
  );
}
