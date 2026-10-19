<script setup lang="ts">
// 首頁：版面依 prototype/blush.html；文字逐字取自舊站首頁（資料見 server/mock/pages/home.ts）
// 首頁只做導覽：服務、醫師、特色、文章、常見問題各放摘要，深入內容連到各自頁面
const [{ data: home }, { data: posts }] = await Promise.all([
  useHomeContent(),
  usePosts({ pageSize: 3 }),
])
if (!home.value) throw createError({ statusCode: 500, message: '首頁內容載入失敗', fatal: true })
const h = home.value

useSiteSeo({ title: h.seo.title, description: h.seo.description, exact: true, path: '/' })

// 服務卡的線條圖示（裝飾用，依卡片順序）
const icons = ['sun', 'needle', 'cup', 'leaf'] as const
</script>

<template>
  <div v-if="home">
    <!-- 主視覺：置中品牌標題 + 拱形診所實景（blush.html） -->
    <section id="top" class="bg-blush-bg" aria-labelledby="hero-title">
      <div class="wrap flex flex-col items-center pt-16 pb-14 text-center md:pt-24">
        <p class="text-[15px] tracking-[0.18em] text-blush-ink sm:text-base">{{ h.hero.tag }}</p>
        <h1 id="hero-title" class="mt-4">
          <span class="block text-lg tracking-[0.2em] text-cocoa sm:text-xl">{{
            h.hero.title
          }}</span>
          <span
            class="mt-2 block font-serif text-[2.25rem] leading-[1.25] font-black tracking-[0.16em] whitespace-nowrap text-rose sm:text-6xl sm:tracking-[0.2em] lg:text-[4.25rem]"
            >{{ h.hero.brand }}</span
          >
        </h1>
        <p class="mt-6 max-w-xl text-[15.5px] leading-8 text-body md:text-[17px]">
          {{ h.hero.lead }}
        </p>
        <div class="mt-9">
          <NuxtLink to="/booking" class="btn-primary">{{ h.hero.ctaLabel }}</NuxtLink>
        </div>

        <div
          class="mt-12 grid w-full max-w-[980px] grid-cols-1 items-end gap-3 sm:mt-14 sm:grid-cols-3 sm:gap-6"
        >
          <ResponsiveImage
            src="reception"
            alt="聖儀堂中醫診所環境"
            sizes="(min-width: 1024px) 310px, 30vw"
            class="arch hidden aspect-[3/4] w-full object-cover sm:block"
          />
          <ResponsiveImage
            src="entrance-door-3"
            alt="聖儀堂中醫診所環境"
            sizes="(min-width: 1024px) 310px, (min-width: 640px) 30vw, 72vw"
            priority
            class="arch mx-auto aspect-[3/4.4] w-[72%] max-w-[320px] object-cover sm:w-full sm:max-w-none"
          />
          <ResponsiveImage
            src="consult-room"
            alt="中醫婦科診症環境"
            sizes="(min-width: 1024px) 310px, 30vw"
            class="arch hidden aspect-[3/4] w-full object-cover sm:block"
          />
        </div>
      </div>
    </section>

    <StatsBar :stats="h.stats" />

    <!-- 全面中醫診療服務 -->
    <section id="services" class="section bg-stone" aria-labelledby="services-title">
      <div class="wrap">
        <SectionHead
          heading-id="services-title"
          :en="h.services.en"
          :title="h.services.title"
          :lead="h.services.lead"
        />
        <ul class="mt-14 grid grid-cols-1 gap-x-8 gap-y-12 sm:grid-cols-2 lg:grid-cols-4">
          <li v-for="(c, i) in h.services.cards" :key="c.title">
            <NuxtLink :to="c.to" class="group flex flex-col items-center text-center">
              <span
                class="grid size-20 place-items-center rounded-full border border-blush bg-paper text-rose transition-colors group-hover:bg-petal sm:size-24"
                aria-hidden="true"
              >
                <svg
                  width="40"
                  height="40"
                  viewBox="0 0 48 48"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.5"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                >
                  <template v-if="icons[i] === 'sun'">
                    <circle cx="24" cy="24" r="7" />
                    <path
                      d="M24 7v5m0 24v5M7 24h5m24 0h5M12 12l3.5 3.5m17 17L36 36M12 36l3.5-3.5m17-17L36 12"
                    />
                  </template>
                  <template v-else-if="icons[i] === 'needle'">
                    <path d="M10 38L30 18" />
                    <path d="M30 18l3-3 4 4-3 3z" />
                    <path d="M34 14l2-2m1 5l2-2m-4-3l1-3m3 7l3-1" />
                    <circle cx="12" cy="36" r="1.2" />
                  </template>
                  <template v-else-if="icons[i] === 'cup'">
                    <path d="M16 14h16v3c4 2 6 6 6 11a14 14 0 0 1-28 0c0-5 2-9 6-11z" />
                    <path d="M16 17h16" />
                    <path d="M6 42h36" />
                  </template>
                  <template v-else>
                    <path d="M12 36C12 20 22 10 38 10c0 16-10 26-26 26z" />
                    <path d="M12 36L28 20" />
                  </template>
                </svg>
              </span>
              <span class="tag mt-5">{{ c.tag }}</span>
              <h3
                class="mt-3 font-sans text-xl font-bold tracking-[0.12em] text-cocoa group-hover:text-rose"
              >
                {{ c.title }}
              </h3>
              <p class="mt-3 max-w-[22em] text-[14.5px] leading-[1.9] text-muted">{{ c.text }}</p>
              <span class="mt-4 text-sm font-medium tracking-[0.06em] text-cocoa">{{
                c.linkLabel
              }}</span>
            </NuxtLink>
          </li>
        </ul>
      </div>
    </section>

    <!-- 關於醫師 -->
    <section id="about" class="section cv-auto" aria-labelledby="about-title">
      <div class="wrap grid items-center gap-12 md:grid-cols-[0.9fr_1.1fr] md:gap-16">
        <div class="relative mx-auto w-full max-w-sm">
          <ResponsiveImage
            src="doctor-pan"
            alt="潘巧儀註冊中醫"
            sizes="(min-width: 768px) 380px, 85vw"
            class="arch aspect-[4/5] w-full object-cover object-top"
          />
        </div>
        <div>
          <SectionHead
            heading-id="about-title"
            :en="h.about.en"
            :title="h.about.title"
            align="left"
          />
          <RichText
            v-for="p in h.about.paragraphs"
            :key="p"
            :text="p"
            tag="p"
            class="mt-6 text-[15.5px] leading-8"
          />
          <ul class="mt-6 space-y-2 text-[14.5px]">
            <li v-for="b in h.about.bullets" :key="b" class="flex items-start gap-2">
              <span aria-hidden="true" class="mt-1 text-blush">✿</span>{{ b }}
            </li>
          </ul>
          <NuxtLink to="/about" class="text-link mt-8">{{ h.about.linkLabel }}</NuxtLink>
        </div>
      </div>
    </section>

    <!-- 為何選擇聖儀堂 -->
    <section id="why" class="section cv-auto bg-blush-bg" aria-labelledby="why-title">
      <div class="wrap">
        <SectionHead
          heading-id="why-title"
          :en="h.why.en"
          :title="h.why.title"
          :lead="h.why.lead"
        />
        <ul class="mt-12 grid gap-6 md:grid-cols-3">
          <li v-for="w in h.why.items" :key="w.title" class="flex flex-col bg-paper p-8">
            <span
              aria-hidden="true"
              class="grid size-14 place-items-center rounded-full border border-blush bg-blush-bg text-2xl"
              >{{ w.icon }}</span
            >
            <h3 class="mt-5 font-sans text-xl font-bold tracking-[0.1em] text-cocoa">
              {{ w.title }}
            </h3>
            <p class="mt-3 text-[15px] leading-[1.9]">{{ w.text }}</p>
          </li>
        </ul>
      </div>
    </section>

    <!-- 中醫健康資訊（最新文章） -->
    <section class="section cv-auto" aria-labelledby="blog-title">
      <div class="wrap">
        <SectionHead
          heading-id="blog-title"
          en="HEALTH JOURNAL"
          title="中醫健康資訊"
          lead="分享中醫婦科、美容、養生及體重管理知識。"
        />
        <ul class="mt-12 grid gap-8 md:grid-cols-2 lg:grid-cols-3">
          <li v-for="p in posts?.items" :key="p.slug"><PostCard :post="p" /></li>
        </ul>
      </div>
    </section>

    <!-- 常見問題（精選；全部問題在 /faq） -->
    <section class="section cv-auto bg-stone">
      <div class="wrap-narrow"><ArticleFaq :faq="h.faq" align="center" /></div>
    </section>

    <CtaBand :cta="h.cta" />
  </div>
</template>
