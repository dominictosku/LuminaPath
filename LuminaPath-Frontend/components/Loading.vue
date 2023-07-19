<template>
  <section class="container px-4 mx-auto">
    <div class="flex flex-col mt-6">
      <div class="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
        <div class="inline-block min-w-full py-2 align-middle md:px-6 lg:px-8">
          <div class="overflow-hidden border border-gray-200 dark:border-gray-700 md:rounded-lg">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th scope="col" class="py-3.5 px-4 text-sm font-normal text-left
                                        rtl:text-right text-gray-500 dark:text-gray-400">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>

                  <th scope="col" class="media-th">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>

                  <th scope="col" class="media-th">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>

                  <th scope="col" class="media-th">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>

                  <th scope="col" class="media-th">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>

                  <th scope="col" class="media-th">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200 dark:divide-gray-700 dark:bg-gray-900">
                <tr>
                  <td class="px-4 py-4 text-sm font-medium whitespace-nowrap">
                    <div>
                      <h2 class="font-medium text-gray-800 dark:text-white ">
                        <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                      </h2>
                      <p class="text-sm font-normal text-gray-600 dark:text-gray-400">
                        <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                      </p>
                    </div>
                  </td>
                  <td class="px-4 py-4 text-sm font-medium whitespace-nowrap">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </td>
                  <td class="px-4 py-4 text-sm font-medium whitespace-nowrap">
                    <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                  </td>
                  <td class="px-4 py-4 text-sm whitespace-nowrap">
                    <div>
                      <h4 class="text-gray-700 dark:text-gray-200">
                        <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                      </h4>
                      <p class="text-gray-500 dark:text-gray-400">
                        <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                      </p>
                    </div>
                  </td>
                  <td class="px-4 py-4 text-sm whitespace-nowrap">
                    <div class="flex items-center">
                      <ion-skeleton-text :animated="true" style="width: 80px"></ion-skeleton-text>
                    </div>
                  </td>

                  <td class="px-4 py-4 text-sm whitespace-nowrap">
                    <ion-progress-bar type="indeterminate"></ion-progress-bar>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>