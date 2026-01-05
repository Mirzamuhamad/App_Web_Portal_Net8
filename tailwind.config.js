module.exports = {
  content: [
    "./Pages/**/*.{cshtml,html,razor}",
    "./Views/**/*.{cshtml,html}",
    "./wwwroot/**/*.js",
    "./wwwroot/**/*.css",
  ],
  safelist: [
    'group',
    'group-hover:block',
    'group-hover:flex',
    'group-hover:opacity-100',
    'group-hover:visible',
    'opacity-0',
    'invisible',
    'translate-y-2',
    'translate-y-0'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Poppins', 'sans-serif'],
        muli: ['Mulish', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
